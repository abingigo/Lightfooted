using System;
using System.Collections.Generic;
using System.IO;
using Lightfooted.Analytics;
using NUnit.Framework;

public class AnalyticsDataTests
{
    List<AnalyticsEvent> events;
    AttemptRecorder recorder;
    string directory;
    [SetUp]
    public void Setup()
    {
        events = new List<AnalyticsEvent>();
        recorder = new AttemptRecorder("session", "test", "test", events.Add);
        directory = Path.Combine(UnityEngine.Application.temporaryCachePath, "AnalyticsTests", Guid.NewGuid().ToString("N"));
    }
    [TearDown]
    public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    [Test]
    public void ActiveClockExcludesPauseAndBackgroundButElapsedIncludesThem()
    {
        recorder.Begin("level"); recorder.Advance(2, true); recorder.Advance(5, false); recorder.Advance(3, true);
        Assert.That(recorder.ActiveSeconds, Is.EqualTo(5)); Assert.That(recorder.ElapsedSeconds, Is.EqualTo(10));
        recorder.End("completed"); recorder.Advance(20, true);
        Assert.That(recorder.ActiveSeconds, Is.EqualTo(5));
    }

    [Test]
    public void CheckpointsAreUniquePerAttemptAndHaveIndependentSplits()
    {
        recorder.Begin("level"); recorder.Advance(2, true);
        Assert.That(recorder.Reach("a"), Is.True); Assert.That(recorder.Reach("a"), Is.False);
        recorder.Advance(3, true); recorder.Reach("b");
        Assert.That(events.Count, Is.EqualTo(3)); Assert.That(events[2].split_seconds, Is.EqualTo(3));
        var first = recorder.AttemptId; recorder.End("restarted"); recorder.Begin("level");
        Assert.That(recorder.AttemptId, Is.Not.EqualTo(first)); Assert.That(recorder.Reach("a"), Is.True);
        Assert.That(events[4].sequence, Is.EqualTo(0));
    }

    [TestCase("completed")]
    [TestCase("died")]
    [TestCase("restarted")]
    [TestCase("left_level")]
    [TestCase("quit")]
    public void AnAttemptEndsExactlyOnce(string result)
    {
        recorder.Begin("level"); Assert.That(recorder.End(result), Is.True);
        Assert.That(recorder.End(result), Is.False); Assert.That(recorder.Reach("a"), Is.False);
        Assert.That(events.Count, Is.EqualTo(2)); Assert.That(events[1].outcome, Is.EqualTo(result));
    }

    [Test]
    public void InvalidClockValuesAreRejected()
    {
        recorder.Begin("level");
        foreach (double value in new[] { double.NaN, double.PositiveInfinity, -1d })
            Assert.Throws<ArgumentOutOfRangeException>(() => recorder.Advance(value, true));
        Assert.Throws<ArgumentException>(() => recorder.End("unknown"));
    }

    [Test]
    public void OfflineQueueSurvivesReloadAndOnlyReceiptsRemoveEvents()
    {
        var queue = new LocalEventQueue(directory);
        recorder = new AttemptRecorder("session", "test", "test", queue.Enqueue);
        recorder.Begin("level"); recorder.Advance(1, true); recorder.Reach("a");
        var loaded = new LocalEventQueue(directory);
        Assert.That(loaded.Count, Is.EqualTo(2));
        Assert.That(loaded.Peek()[0].sequence, Is.EqualTo(0));
        loaded.Acknowledge(new[] { loaded.Peek()[0].event_id });
        Assert.That(new LocalEventQueue(directory).Count, Is.EqualTo(1));
        Assert.That(new LocalEventQueue(directory).Peek()[0].checkpoint_id, Is.EqualTo("a"));
    }

    [Test]
    public void CorruptAndIncompleteFilesAreRetainedWithoutLosingGoodEvents()
    {
        var queue = new LocalEventQueue(directory);
        recorder.Begin("level"); queue.Enqueue(events[0]);
        File.WriteAllText(Path.Combine(directory, "damaged.json"), "{broken");
        File.WriteAllText(Path.Combine(directory, "unfinished.json.tmp"), "{");
        var loaded = new LocalEventQueue(directory);
        Assert.That(loaded.Count, Is.EqualTo(1)); Assert.That(loaded.LastError, Is.Not.Empty);
        Assert.That(File.Exists(Path.Combine(directory, "damaged.json")), Is.True);
    }

    [Test]
    public void StorageFailureRetainsEventsInMemoryAndRecoversAfterDirectoryReturns()
    {
        Directory.CreateDirectory(directory);
        string blocked = Path.Combine(directory, "blocked"); File.WriteAllText(blocked, "file occupies directory path");
        var queue = new LocalEventQueue(blocked);
        recorder.Begin("level"); Assert.DoesNotThrow(() => queue.Enqueue(events[0]));
        Assert.That(queue.Count, Is.EqualTo(1)); Assert.That(queue.LastError, Is.Not.Empty);
        File.Delete(blocked); Assert.That(queue.Persist(), Is.True);
        Assert.That(new LocalEventQueue(blocked).Count, Is.EqualTo(1));
    }
}
