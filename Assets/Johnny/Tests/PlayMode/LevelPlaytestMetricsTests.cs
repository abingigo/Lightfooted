using System;
using System.Collections.Generic;
using System.Linq;
using LightPlatformer.UI;
using NUnit.Framework;

public class LevelPlaytestMetricsTests
{
    readonly List<(string name, (string key, object value)[] data)> events = new List<(string, (string, object)[])>();
    LevelPlaytestMetrics metrics;
    [SetUp] public void SetUp()
    {
        events.Clear();
        metrics = new LevelPlaytestMetrics((name, data) => events.Add((name, data)), "test-build");
    }
    object Value(string eventName, string key) => events.Single(e => e.name == eventName).data.Single(p => p.key == key).value;

    [Test] public void CompletionReportsOnlyActiveSecondsAsANumber()
    {
        metrics.Begin("Level 1"); metrics.Advance(2.5, true); metrics.Advance(100, false); metrics.Advance(3, true);
        Assert.That(metrics.Finish(true), Is.True);
        Assert.That(Value("level_completed", "active_seconds"), Is.TypeOf<double>().And.EqualTo(5.5));
        Assert.That(Value("level_completed", "level"), Is.EqualTo("Level 1"));
        Assert.That(Value("level_completed", "build_version"), Is.EqualTo("test-build"));
        Assert.That(events.Count(e => e.name == "level_started"), Is.EqualTo(1));
    }

    [Test] public void RepeatedOutcomesAndRestartAfterOutcomeCannotDuplicateEvents()
    {
        metrics.Begin("TutorialLevel"); metrics.Finish(true);
        Assert.That(metrics.Finish(true), Is.False);
        Assert.That(metrics.Finish(false), Is.False);
        Assert.That(metrics.Restart(), Is.False);
        Assert.That(events.Select(e => e.name), Is.EqualTo(new[] { "level_started", "level_completed" }));
    }

    [Test] public void DeathDoesNotAddASuccessfulCompletionTime()
    {
        metrics.Begin("Level 2"); metrics.Advance(7, true); metrics.Finish(false);
        Assert.That(events.Any(e => e.name == "level_completed"), Is.False);
        Assert.That(events.Single(e => e.name == "level_failed").data.Any(p => p.key == "active_seconds"), Is.False);
    }

    [Test] public void ManualRestartEndsOneAttemptAndNewLevelStartsANewClock()
    {
        metrics.Begin("Level 2"); metrics.Advance(9, true);
        Assert.That(metrics.Restart(), Is.True); Assert.That(metrics.Restart(), Is.False);
        metrics.Begin("Level 2"); metrics.Advance(2, true); metrics.Finish(true);
        Assert.That(Value("level_completed", "active_seconds"), Is.EqualTo(2));
        Assert.That(events.Count(e => e.name == "level_started"), Is.EqualTo(2));
        Assert.That(events.Count(e => e.name == "level_restarted"), Is.EqualTo(1));
    }

    [Test] public void LeavingOrLoadingDoesNotCountAsRestartOrDeath()
    {
        metrics.Begin("Level 1"); metrics.Leave(); metrics.Advance(50, true);
        Assert.That(metrics.Finish(false), Is.False); Assert.That(metrics.Restart(), Is.False);
        Assert.That(events.Count, Is.EqualTo(1)); Assert.That(metrics.ActiveSeconds, Is.Zero);
    }

    [Test] public void InvalidClockValuesCannotReachAnalytics()
    {
        metrics.Begin("Level 1");
        foreach (double value in new[] { -1d, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => metrics.Advance(value, true));
        metrics.Finish(true); Assert.That(Value("level_completed", "active_seconds"), Is.EqualTo(0));
    }
}
