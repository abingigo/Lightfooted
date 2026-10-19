using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lightfooted.Analytics
{
    // Browser storage is synchronous: saving does not depend on Unity's asynchronous IDBFS flush.
    public sealed class BrowserEventQueue : IEventQueue
    {
        readonly Func<string> read;
        readonly Func<string, bool> write;
        readonly List<AnalyticsEvent> pending = new List<AnalyticsEvent>();
        bool unreadable;
        public int Count => pending.Count;
        public string LastError { get; private set; }

        public BrowserEventQueue(Func<string> read, Func<string, bool> write)
        {
            this.read = read; this.write = write;
            try
            {
                string saved = read();
                if (string.IsNullOrEmpty(saved)) return;
                var batch = JsonUtility.FromJson<EventBatch>(saved);
                if (batch?.events == null) throw new ArgumentException("Invalid saved event batch.");
                foreach (var item in batch.events)
                {
                    if (item == null || !Guid.TryParseExact(item.event_id, "N", out _))
                        throw new ArgumentException("Invalid saved event ID.");
                    pending.Add(item);
                }
            }
            catch (Exception)
            {
                pending.Clear(); unreadable = true;
                LastError = "Saved browser queue is unreadable; it was preserved. New events remain in memory.";
            }
        }

        public void Enqueue(AnalyticsEvent data)
        {
            if (data == null || !Guid.TryParseExact(data.event_id, "N", out _)) throw new ArgumentException("Invalid event ID.");
            pending.Add(data); Persist();
        }

        public bool Persist()
        {
            if (unreadable) return false;
            try
            {
                if (!write(JsonUtility.ToJson(new EventBatch { events = pending.ToArray() })))
                    throw new InvalidOperationException("Browser storage is unavailable or full.");
                LastError = null; return true;
            }
            catch (Exception)
            { LastError = "Browser storage is unavailable or full; pending events remain in memory."; return false; }
        }

        public AnalyticsEvent[] Peek(int limit = 32) => pending.GetRange(0, Math.Min(limit, pending.Count)).ToArray();

        public bool Acknowledge(string[] ids)
        {
            if (ids == null) return false;
            var accepted = new HashSet<string>(ids, StringComparer.Ordinal);
            var previous = pending.ToArray();
            pending.RemoveAll(item => accepted.Contains(item.event_id));
            if (Persist()) return true;
            pending.Clear(); pending.AddRange(previous);
            return false;
        }
    }
}
