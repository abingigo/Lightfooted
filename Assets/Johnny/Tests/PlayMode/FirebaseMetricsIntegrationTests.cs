using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LightPlatformer.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class FirebaseMetricsIntegrationTests
{
    const string Lab = "Assets/Scenes/Johnny/AnalyticsLab.unity";
    readonly List<string> events = new List<string>();
    GameMenuController Menu => GameMenuController.Instance;
    void Capture(string message, string stack, LogType type) { if (message.StartsWith("[Analytics] ")) events.Add(message); }
    int Count(string name) => events.Count(e => e.StartsWith("[Analytics] " + name + " "));

    [UnitySetUp] public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync(LevelCatalog.MenuPath);
        yield return null;
        events.Clear(); Application.logMessageReceived += Capture;
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        Application.logMessageReceived -= Capture;
        Menu.SendMessage("OnApplicationPause", false);
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync(LevelCatalog.MenuPath);
    }
    IEnumerator OpenLab()
    {
        yield return SceneManager.LoadSceneAsync(Lab);
        yield return null; yield return null;
        Assert.That(Menu.Metrics.IsRunning, Is.True);
    }
    IEnumerator WaitForLoad()
    {
        float deadline = Time.realtimeSinceStartup + 10;
        while (Menu.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Menu.IsLoading, Is.False);
        yield return null;
    }

    [UnityTest] public IEnumerator PauseAndSuspensionDoNotIncreaseActiveTime()
    {
        yield return OpenLab();
        Menu.Pause(); double paused = Menu.Metrics.ActiveSeconds;
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(Menu.Metrics.ActiveSeconds, Is.EqualTo(paused).Within(0.02));
        Menu.Resume(); yield return null;
        Menu.SendMessage("OnApplicationPause", true); double hidden = Menu.Metrics.ActiveSeconds;
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(Menu.Metrics.ActiveSeconds, Is.EqualTo(hidden).Within(0.02));
        Menu.SendMessage("OnApplicationPause", false);
    }

    [UnityTest] public IEnumerator CompletionAndFailureCallbacksAreDeduplicated()
    {
        yield return OpenLab();
        Assert.That(Count("level_started"), Is.EqualTo(1));
        GameMenuController.NotifyVictory(); GameMenuController.NotifyVictory(); GameMenuController.NotifyGameOver();
        Assert.That(Count("level_completed"), Is.EqualTo(1)); Assert.That(Count("level_failed"), Is.Zero);
        Assert.That(events.Single(e => e.Contains("level_completed")), Does.Contain("\"active_seconds\":").And.Contain("\"level\":\"AnalyticsLab\""));
        Assert.That(Menu.Restart(), Is.True); yield return WaitForLoad();
        Assert.That(Count("level_restarted"), Is.Zero, "Retrying a completed attempt is not a manual abandon.");
        GameMenuController.NotifyGameOver(); GameMenuController.NotifyGameOver();
        Assert.That(Count("level_failed"), Is.EqualTo(1));
    }

    [UnityTest] public IEnumerator PausedRestartReportsOnceAndResetsTime()
    {
        yield return OpenLab();
        Menu.Pause(); Menu.Metrics.Advance(20, true);
        Assert.That(Menu.Restart(), Is.True); Assert.That(Menu.Restart(), Is.False);
        yield return WaitForLoad();
        Assert.That(Count("level_restarted"), Is.EqualTo(1)); Assert.That(Count("level_started"), Is.EqualTo(2));
        Assert.That(Menu.Metrics.ActiveSeconds, Is.LessThan(2));
    }

    [UnityTest] public IEnumerator MenuAndRejectedLoadsCannotProduceRestarts()
    {
        Assert.That(Menu.Metrics.IsRunning, Is.False); Assert.That(Count("level_started"), Is.Zero);
        yield return OpenLab();
        Assert.That(Menu.LoadLevel("Assets/Scenes/MissingLevel.unity"), Is.False);
        Assert.That(Menu.Metrics.IsRunning, Is.True);
        Assert.That(Menu.GoToMenu(), Is.True); yield return WaitForLoad();
        Assert.That(Menu.Metrics.IsRunning, Is.False); Assert.That(Count("level_restarted"), Is.Zero);
        Assert.That(Count("level_completed") + Count("level_failed"), Is.Zero);
    }

    [UnityTest] public IEnumerator LegacyCollectorDoesNotStartInTheDefaultGame()
    {
        yield return OpenLab();
        if (System.Environment.GetEnvironmentVariable("LIGHTFOOTED_LEGACY_ANALYTICS") == "1") Assert.Ignore("Legacy collector was explicitly enabled for its old tests.");
        var legacy = System.Type.GetType("AnalyticsService, Assembly-CSharp", true);
        Assert.That(legacy.GetProperty("Instance").GetValue(null), Is.Null);
    }
}
