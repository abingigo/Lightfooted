using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lightfooted.Analytics;
using LightPlatformer.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Networking;

public class AnalyticsIntegrationTests
{
    [Serializable] public class Summary { public Attempt[] attempts; }
    [Serializable] public class Attempt
    {
        public string attempt_id, outcome;
        public double active_seconds, elapsed_seconds;
        public int checkpoints;
    }
    const string Lab = "Assets/Scenes/Johnny/AnalyticsLab.unity";
    static Type ServiceType => Type.GetType("AnalyticsService, Assembly-CSharp", true);
    static object Service => ServiceType.GetProperty("Instance").GetValue(null);
    static AttemptRecorder Recorder => (AttemptRecorder)ServiceType.GetProperty("Recorder").GetValue(Service);
    static bool Reach(string id) => (bool)ServiceType.GetMethod("ReachCheckpoint").Invoke(Service, new object[] { id });

    [UnitySetUp]
    public IEnumerator EnableLegacyCollectorForThisFixture()
    {
        if (Service == null) new GameObject("Legacy analytics test service").AddComponent(ServiceType);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator RemoveLegacyCollectorAfterThisFixture()
    {
        if (Service is Component component) UnityEngine.Object.Destroy(component.gameObject);
        yield return null;
    }

    static IEnumerator Open(string path)
    {
        Time.timeScale = 1;
        var load = SceneManager.LoadSceneAsync(path);
        while (!load.isDone) yield return null;
        yield return null; yield return null;
    }

    [UnityTest]
    public IEnumerator PauseFreezesActiveClockAndBlocksCheckpoint()
    {
        yield return Open(Lab);
        Assert.That(Service, Is.Not.Null); Assert.That(Recorder.IsRunning, Is.True);
        GameMenuController.Instance.Pause();
        double active = Recorder.ActiveSeconds, elapsed = Recorder.ElapsedSeconds;
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(Recorder.ActiveSeconds, Is.EqualTo(active).Within(0.02));
        Assert.That(Recorder.ElapsedSeconds, Is.GreaterThan(elapsed + 0.15));
        Assert.That(Reach("paused-checkpoint"), Is.False);
        GameMenuController.Instance.Resume(); yield return null;
        Assert.That(Reach("resumed-checkpoint"), Is.True);
    }

    [UnityTest]
    public IEnumerator CheckpointUsesBodyGeometryAndDeduplicatesSensorContacts()
    {
        yield return Open(Lab);
        var recorded = new List<AnalyticsEvent>(); Recorder.Recorded += recorded.Add;
        var player = GameObject.FindGameObjectWithTag("Player");
        var body = player.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        var checkpoint = GameObject.Find("Checkpoint gate-a");
        var component = checkpoint.GetComponent(Type.GetType("AnalyticsCheckpoint, Assembly-CSharp", true));
        var contact = component.GetType().GetMethod("OnTriggerStay2D", BindingFlags.Instance | BindingFlags.NonPublic);
        player.transform.position = new Vector3(-2, 1, 0); Physics2D.SyncTransforms();
        contact.Invoke(component, new object[] { player.GetComponent<CircleCollider2D>() });
        Assert.That(recorded.Count, Is.Zero, "A light sensor must not record early progress.");
        player.transform.position = new Vector3(5, 1, 0); Physics2D.SyncTransforms();
        // Invoke the actual contact callback with both colliders, including a disabled body in darkness.
        player.GetComponent<BoxCollider2D>().enabled = false;
        contact.Invoke(component, new object[] { player.GetComponent<CircleCollider2D>() });
        contact.Invoke(component, new object[] { player.GetComponent<BoxCollider2D>() });
        Assert.That(recorded.Count(e => e.event_type == "checkpoint_reached" && e.checkpoint_id == "gate-a"), Is.EqualTo(1));
        Recorder.Recorded -= recorded.Add;
    }

    [UnityTest]
    public IEnumerator PhysicsOverlapRecordsARealCheckpointOnlyOnce()
    {
        yield return Open(Lab);
        var recorded = new List<AnalyticsEvent>(); Recorder.Recorded += recorded.Add;
        var player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        player.transform.position = new Vector3(12, 1, 0); Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(recorded.Count(e => e.checkpoint_id == "gate-b"), Is.EqualTo(1));
        Recorder.Recorded -= recorded.Add;
    }

    [UnityTest]
    public IEnumerator RestartAndReturnToMenuHaveCorrectOutcomes()
    {
        yield return Open(Lab);
        var recorded = new List<AnalyticsEvent>(); Recorder.Recorded += recorded.Add;
        string old = Recorder.AttemptId;
        Assert.That(GameMenuController.Instance.Restart(), Is.True);
        int frames = 0;
        while (Recorder.AttemptId == old && frames++ < 300) yield return null;
        Assert.That(Recorder.AttemptId, Is.Not.EqualTo(old));
        Assert.That(recorded.Count(e => e.outcome == "restarted"), Is.EqualTo(1));
        Assert.That(GameMenuController.Instance.GoToMenu(), Is.True);
        while (GameMenuController.Instance.State == MenuState.Loading) yield return null;
        Assert.That(Recorder.IsRunning, Is.False);
        Assert.That(recorded.Count(e => e.outcome == "left_level"), Is.EqualTo(1));
        Recorder.Recorded -= recorded.Add;
    }

    [UnityTest]
    public IEnumerator CompletionAndDeathEndAttemptsWithoutDuplicateEnds()
    {
        yield return Open(Lab);
        var recorded = new List<AnalyticsEvent>(); Recorder.Recorded += recorded.Add;
        GameMenuController.NotifyVictory(); GameMenuController.NotifyVictory(); yield return null;
        Assert.That(recorded.Count(e => e.outcome == "completed"), Is.EqualTo(1));
        Assert.That(Recorder.IsRunning, Is.False);
        yield return Open(Lab);
        GameMenuController.NotifyGameOver(); GameMenuController.NotifyGameOver(); yield return null;
        Assert.That(recorded.Count(e => e.outcome == "died"), Is.EqualTo(1));
        Assert.That(Recorder.IsRunning, Is.False);
        Recorder.Recorded -= recorded.Add;
    }

    [UnityTest, Category("LocalCollector")]
    public IEnumerator CompletedPhysicsRouteReachesSqliteAfterOfflineQueueing()
    {
        if (Environment.GetEnvironmentVariable("LIGHTFOOTED_ANALYTICS_E2E") != "1")
            Assert.Ignore("Opt-in check: set LIGHTFOOTED_ANALYTICS_E2E=1, start this test with the collector offline, then start the collector when offline-ready.txt appears.");
        yield return Open(Lab);
        string attemptId = Recorder.AttemptId;
        var player = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        var movement = player.GetComponents<MonoBehaviour>().Single(item => item.GetType().Name == "PlayerController");
        movement.enabled = false;
        bool jumped = false, paused = false;
        double pauseActive = 0;
        float timeout = Time.realtimeSinceStartup + 20;
        while (GameMenuController.Instance.State == MenuState.Playing && Time.realtimeSinceStartup < timeout)
        {
            if (!paused && player.position.x > 12.8f)
            {
                GameMenuController.Instance.Pause(); pauseActive = Recorder.ActiveSeconds;
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(Recorder.ActiveSeconds, Is.EqualTo(pauseActive).Within(0.02));
                GameMenuController.Instance.Resume(); paused = true; yield return null; yield return null;
            }
            bool jump = !jumped && player.position.x >= 15 && Physics2D.Raycast(player.position, Vector2.down, 0.7f, LayerMask.GetMask("Ground"));
            if (jump) jumped = true;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            movement.GetType().GetField("moveX", flags).SetValue(movement, 1f);
            movement.GetType().GetField("jumpQueued", flags).SetValue(movement, jump);
            movement.SendMessage("FixedUpdate");
            yield return new WaitForFixedUpdate();
        }
        Assert.That(jumped && paused, Is.True);
        Assert.That(GameMenuController.Instance.State, Is.EqualTo(MenuState.Victory));
        var queue = (LocalEventQueue)ServiceType.GetProperty("Queue").GetValue(Service);
        Assert.That(queue.Peek().Count(e => e.attempt_id == attemptId), Is.EqualTo(4), "Start, two markers, end must remain queued while offline.");
        Assert.That(new LocalEventQueue(queue.DirectoryPath).Count, Is.GreaterThanOrEqualTo(4), "Events must survive a fresh queue reader.");
        System.IO.Directory.CreateDirectory("tmp/AnalyticsReview");
        System.IO.File.WriteAllText("tmp/AnalyticsReview/offline-ready.txt", attemptId);
        Attempt saved = null;
        timeout = Time.realtimeSinceStartup + 120;
        while (saved == null && Time.realtimeSinceStartup < timeout)
        {
            using (var request = UnityWebRequest.Get("http://127.0.0.1:8765/summary"))
            {
                request.timeout = 2;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    saved = JsonUtility.FromJson<Summary>(request.downloadHandler.text).attempts?.FirstOrDefault(item => item.attempt_id == attemptId && item.outcome == "completed");
            }
            if (saved == null) yield return new WaitForSecondsRealtime(0.5f);
        }
        Assert.That(saved, Is.Not.Null, "Start the local collector after offline-ready.txt appears.");
        Assert.That(saved.checkpoints, Is.EqualTo(2));
        Assert.That(saved.active_seconds, Is.EqualTo(Recorder.ActiveSeconds).Within(0.01));
        Assert.That(saved.elapsed_seconds, Is.GreaterThan(saved.active_seconds + 0.25));
        System.IO.File.WriteAllText("tmp/AnalyticsReview/e2e-attempt.json", JsonUtility.ToJson(saved, true));
        timeout = Time.realtimeSinceStartup + 8;
        while (queue.Count > 0 && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(queue.Count, Is.Zero, "Confirmed events should leave the outbox.");
    }
}
