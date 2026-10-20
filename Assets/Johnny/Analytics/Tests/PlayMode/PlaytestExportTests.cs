using Lightfooted.Analytics;
using NUnit.Framework;
using UnityEngine;

public class PlaytestExportTests
{
    [Test]
    public void ExportIncludesMoreThanOneUploadBatchWithoutClearingData()
    {
        string saved = null;
        var queue = new BrowserEventQueue(() => saved, value => { saved = value; return true; });
        for (int i = 0; i < 40; i++)
        {
            var recorder = new AttemptRecorder("session", "build", "WebGLPlayer", queue.Enqueue);
            recorder.Begin("level"); recorder.Advance(2, true); recorder.End("died");
        }
        var export = PlaytestExport.Capture(queue, "qa");
        Assert.That(export.events.Length, Is.EqualTo(80));
        Assert.That(queue.Count, Is.EqualTo(80));
        Assert.That(new BrowserEventQueue(() => saved, _ => true).Count, Is.EqualTo(80));
        var parsed = JsonUtility.FromJson<PlaytestExport>(export.ToJson());
        Assert.That(parsed.format, Is.EqualTo("lightfooted-playtest"));
        Assert.That(parsed.schema_version, Is.EqualTo(1));
        Assert.That(parsed.dataset, Is.EqualTo("qa"));
        Assert.That(parsed.events[79].outcome, Is.EqualTo("died"));
    }

    [Test]
    public void DownloadSnapshotDoesNotFinishAnActiveAttempt()
    {
        var queue = new BrowserEventQueue(() => null, _ => false);
        var recorder = new AttemptRecorder("session", "build", "WebGLPlayer", queue.Enqueue);
        recorder.Begin("level"); recorder.Advance(5, true); recorder.Reach("gate");
        var export = PlaytestExport.Capture(queue, "qa");
        Assert.That(recorder.IsRunning, Is.True);
        Assert.That(export.events.Length, Is.EqualTo(2));
        Assert.That(export.events[1].active_seconds, Is.EqualTo(5));
        Assert.That(queue.Count, Is.EqualTo(2), "Storage failure must still allow a memory snapshot.");
        recorder.End("completed");
        Assert.That(PlaytestExport.Capture(queue, "qa").events.Length, Is.EqualTo(3));
    }

    [Test]
    public void EmptySnapshotHasAnEmptyArrayAndCannotInventObservations()
    {
        var export = PlaytestExport.Capture(new BrowserEventQueue(() => null, _ => true), "qa");
        Assert.That(export.events, Is.Empty);
        Assert.That(export.ToJson(), Does.Contain("\"events\":[]"));
    }
}
