using System.Collections.Generic;
using Lightfooted.Analytics;
using NUnit.Framework;

public class BrowserQueueTests
{
    static AnalyticsEvent Sample()
    {
        var events = new List<AnalyticsEvent>();
        new AttemptRecorder("session", "version", "WebGLPlayer", events.Add).Begin("level");
        return events[0];
    }

    [Test]
    public void BrowserQueueSurvivesReloadAndReceipt()
    {
        string storage = null;
        var queue = new BrowserEventQueue(() => storage, value => { storage = value; return true; });
        var data = Sample(); queue.Enqueue(data);
        var reloaded = new BrowserEventQueue(() => storage, value => { storage = value; return true; });
        Assert.That(reloaded.Count, Is.EqualTo(1));
        Assert.That(reloaded.Peek()[0].event_id, Is.EqualTo(data.event_id));
        Assert.That(reloaded.Acknowledge(new[] { data.event_id }), Is.True);
        Assert.That(new BrowserEventQueue(() => storage, _ => true).Count, Is.Zero);
    }

    [Test]
    public void FullStorageRetainsMemoryAndRecovers()
    {
        string storage = null; bool available = false;
        var queue = new BrowserEventQueue(() => storage, value => { if (!available) return false; storage = value; return true; });
        queue.Enqueue(Sample()); Assert.That(queue.Count, Is.EqualTo(1)); Assert.That(queue.LastError, Is.Not.Empty);
        available = true; Assert.That(queue.Persist(), Is.True);
        Assert.That(new BrowserEventQueue(() => storage, _ => true).Count, Is.EqualTo(1));
    }

    [Test]
    public void FailedReceiptSaveRestoresPendingIdsForSafeRetry()
    {
        string storage = null; bool available = true;
        var queue = new BrowserEventQueue(() => storage, value => { if (!available) return false; storage = value; return true; });
        var data = Sample(); queue.Enqueue(data); available = false;
        Assert.That(queue.Acknowledge(new[] { data.event_id }), Is.False);
        Assert.That(queue.Count, Is.EqualTo(1));
        Assert.That(new BrowserEventQueue(() => storage, _ => true).Count, Is.EqualTo(1));
    }

    [Test]
    public void CorruptStorageIsPreservedWhileNewEventsStayInMemory()
    {
        string storage = "{broken";
        var queue = new BrowserEventQueue(() => storage, value => { storage = value; return true; });
        Assert.DoesNotThrow(() => queue.Enqueue(Sample()));
        Assert.That(queue.Count, Is.EqualTo(1)); Assert.That(storage, Is.EqualTo("{broken"));
        Assert.That(queue.LastError, Is.Not.Empty);
    }
}
