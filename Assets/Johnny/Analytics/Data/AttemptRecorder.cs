using System;
using System.Collections.Generic;

namespace Lightfooted.Analytics
{
    public sealed class AttemptRecorder
    {
        readonly Action<AnalyticsEvent> emit;
        readonly string session, version, platform;
        readonly HashSet<string> reached = new HashSet<string>(StringComparer.Ordinal);
        string level, attempt;
        int sequence;
        double previousCheckpoint;
        public bool IsRunning { get; private set; }
        public double ActiveSeconds { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public string AttemptId => attempt;
        public event Action<AnalyticsEvent> Recorded;

        public AttemptRecorder(string session, string version, string platform, Action<AnalyticsEvent> emit)
        {
            this.session = session; this.version = version; this.platform = platform;
            this.emit = emit ?? throw new ArgumentNullException(nameof(emit));
        }

        public void Begin(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId)) throw new ArgumentException("A stable level ID is required.");
            if (IsRunning) End("left_level");
            level = levelId; attempt = Guid.NewGuid().ToString("N"); sequence = 0;
            ActiveSeconds = ElapsedSeconds = previousCheckpoint = 0;
            reached.Clear(); IsRunning = true;
            Emit("level_started");
        }

        public void Advance(double seconds, bool activelyPlaying)
        {
            if (!IsRunning) return;
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            ElapsedSeconds += seconds;
            if (activelyPlaying) ActiveSeconds += seconds;
        }

        public bool Reach(string checkpointId)
        {
            if (!IsRunning || string.IsNullOrWhiteSpace(checkpointId) || checkpointId.Length > 512 || !reached.Add(checkpointId)) return false;
            Emit("checkpoint_reached", checkpointId, "", ActiveSeconds - previousCheckpoint);
            previousCheckpoint = ActiveSeconds;
            return true;
        }

        public bool End(string outcome)
        {
            if (!IsRunning) return false;
            if (outcome != "completed" && outcome != "died" && outcome != "restarted" && outcome != "left_level" && outcome != "quit")
                throw new ArgumentException("Unknown attempt outcome.", nameof(outcome));
            Emit("level_ended", "", outcome, ActiveSeconds - previousCheckpoint);
            IsRunning = false;
            return true;
        }

        void Emit(string kind, string checkpoint = "", string outcome = "", double split = 0)
        {
            var data = new AnalyticsEvent {
                event_id = Guid.NewGuid().ToString("N"), session_id = session, attempt_id = attempt,
                sequence = sequence++, event_type = kind, level_id = level, checkpoint_id = checkpoint,
                outcome = outcome, active_seconds = ActiveSeconds, elapsed_seconds = ElapsedSeconds,
                split_seconds = split, recorded_utc = DateTime.UtcNow.ToString("O"), build_version = version, platform = platform
            };
            emit(data);
            Recorded?.Invoke(data);
        }
    }
}
