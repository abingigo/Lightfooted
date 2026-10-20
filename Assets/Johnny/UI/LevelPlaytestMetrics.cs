using System;

namespace LightPlatformer.UI
{
    public sealed class LevelPlaytestMetrics
    {
        readonly Action<string, (string key, object value)[]> track;
        readonly string build;
        string level;
        public bool IsRunning { get; private set; }
        public double ActiveSeconds { get; private set; }

        public LevelPlaytestMetrics(Action<string, (string key, object value)[]> track, string build)
        {
            this.track = track ?? throw new ArgumentNullException(nameof(track));
            this.build = build;
        }

        public void Begin(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName)) throw new ArgumentException("A level name is required.");
            level = levelName;
            ActiveSeconds = 0;
            IsRunning = true;
            track("level_started", new[] { ("level", (object)level), ("build_version", (object)build) });
        }

        public void Advance(double seconds, bool playing)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (IsRunning && playing) ActiveSeconds += seconds;
        }

        public bool Finish(bool completed)
        {
            if (!IsRunning) return false;
            IsRunning = false;
            if (completed)
                track("level_completed", new[] {
                    ("level", (object)level), ("build_version", (object)build),
                    ("active_seconds", (object)Math.Round(ActiveSeconds, 3))
                });
            else
                track("level_failed", new[] { ("level", (object)level), ("build_version", (object)build) });
            return true;
        }

        public bool Restart()
        {
            if (!IsRunning) return false;
            IsRunning = false;
            track("level_restarted", new[] { ("level", (object)level), ("build_version", (object)build) });
            return true;
        }

        public void Leave() { IsRunning = false; }
    }
}
