using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lightfooted.Analytics
{
    // Each event is atomically moved into place before delivery. A receipt removes it.
    // A lost response or interrupted removal causes a safe retry of the same event ID.
    public sealed class LocalEventQueue : IEventQueue
    {
        readonly string directory;
        readonly List<AnalyticsEvent> pending = new List<AnalyticsEvent>();
        public int Count => pending.Count;
        public string LastError { get; private set; }
        public string DirectoryPath => directory;

        public LocalEventQueue(string directory)
        {
            this.directory = directory;
            try
            {
                Directory.CreateDirectory(directory);
                var files = Directory.GetFiles(directory, "*.json");
                Array.Sort(files, StringComparer.Ordinal);
                foreach (var file in files)
                {
                    try
                    {
                        var data = JsonUtility.FromJson<AnalyticsEvent>(File.ReadAllText(file));
                        if (data == null || !Guid.TryParseExact(data.event_id, "N", out _) || string.IsNullOrEmpty(data.attempt_id) || string.IsNullOrEmpty(data.event_type))
                            throw new InvalidDataException("Invalid saved analytics event.");
                        pending.Add(data);
                    }
                    catch (Exception error) when (IsStorageError(error) || error is ArgumentException)
                    { LastError = "Unreadable queued event retained: " + Path.GetFileName(file); }
                }
            }
            catch (Exception error) when (IsStorageError(error)) { LastError = error.Message; }
        }

        static bool IsStorageError(Exception error) => error is IOException || error is UnauthorizedAccessException;
        string EventFile(AnalyticsEvent data) => Path.Combine(directory, data.event_id + ".json");

        public void Enqueue(AnalyticsEvent data)
        {
            if (data == null || !Guid.TryParseExact(data.event_id, "N", out _)) throw new ArgumentException("Invalid event ID.");
            pending.Add(data);
            Persist();
        }

        public bool Persist()
        {
            try
            {
                Directory.CreateDirectory(directory);
                foreach (var data in pending)
                {
                    string path = EventFile(data);
                    if (File.Exists(path)) continue;
                    var bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
                    using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                    { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                    File.Move(path + ".tmp", path);
                }
                return true;
            }
            catch (Exception error) when (IsStorageError(error)) { LastError = error.Message; return false; }
        }

        public AnalyticsEvent[] Peek(int limit = 32)
        {
            // Sequence order is restored after a process restart; delivery itself is idempotent.
            var ordered = new List<AnalyticsEvent>(pending);
            ordered.Sort((a, b) => {
                int time = string.CompareOrdinal(a.recorded_utc, b.recorded_utc);
                return time != 0 ? time : a.sequence.CompareTo(b.sequence);
            });
            return ordered.GetRange(0, Math.Min(limit, ordered.Count)).ToArray();
        }

        public bool Acknowledge(string[] ids)
        {
            if (ids == null) return false;
            var received = new HashSet<string>(ids, StringComparer.Ordinal);
            try
            {
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    if (!received.Contains(pending[i].event_id)) continue;
                    File.Delete(EventFile(pending[i]));
                    pending.RemoveAt(i);
                }
                return true;
            }
            catch (Exception error) when (IsStorageError(error)) { LastError = error.Message; return false; }
        }
    }
}
