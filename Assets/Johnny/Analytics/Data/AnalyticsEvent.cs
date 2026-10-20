using System;

namespace Lightfooted.Analytics
{
    [Serializable]
    public sealed class AnalyticsEvent
    {
        public string event_id;
        public string session_id;
        public string attempt_id;
        public int sequence;
        public int schema_version = 1;
        public string event_type;
        public string level_id;
        public string checkpoint_id = "";
        public string outcome = "";
        public double active_seconds;
        public double elapsed_seconds;
        public double split_seconds;
        public string recorded_utc;
        public string build_version;
        public string platform;
    }

    [Serializable]
    public sealed class EventBatch { public AnalyticsEvent[] events; }

    [Serializable]
    public sealed class EventReceipt { public string[] accepted_ids; }
}
