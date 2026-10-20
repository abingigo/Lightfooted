using System;
using UnityEngine;

namespace Lightfooted.Analytics
{
    [Serializable]
    public sealed class PlaytestExport
    {
        public string format = "lightfooted-playtest";
        public int schema_version = 1;
        public string exported_utc;
        public string dataset;
        public AnalyticsEvent[] events;

        // Download is a snapshot. Keep saved events so a cancelled download cannot lose data.
        public static PlaytestExport Capture(IEventQueue queue, string dataset)
        {
            if (queue == null) throw new InvalidOperationException("Playtest data is still loading.");
            queue.Persist();
            return new PlaytestExport {
                exported_utc = DateTime.UtcNow.ToString("O"), dataset = dataset,
                events = queue.Peek(queue.Count)
            };
        }

        public string ToJson() => JsonUtility.ToJson(this);
    }
}
