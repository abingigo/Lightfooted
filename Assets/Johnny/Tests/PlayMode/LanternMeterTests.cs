using System;
using System.Collections;
using System.IO;
using System.Linq;
using LightPlatformer.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class LanternMeterTests
{
    const string Level = "Assets/Scenes/Level 4.unity";
    GameMenuController Menu => GameMenuController.Instance;
    GameObject player;
    MonoBehaviour timer;
    ILanternStatus Status => (ILanternStatus)timer;
    LanternMeter Meter => Object.FindFirstObjectByType<LanternMeter>();
    float Fill => Meter.transform.Find("Time bar/Time fill").GetComponent<RectTransform>().anchorMax.x;
    float Alpha => Meter.GetComponent<CanvasGroup>().alpha;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        CheckpointProgress.reachedCheckpoint = false;
        yield return SceneManager.LoadSceneAsync(Level);
        yield return null;
        yield return null;
        FindPlayer();
    }

    void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        foreach (var behaviour in player.GetComponents<MonoBehaviour>())
            if (behaviour.GetType().Name == "PlayerController") behaviour.enabled = false;
        player.GetComponent<Rigidbody2D>().simulated = false;
        timer = player.GetComponentsInChildren<MonoBehaviour>(true)
            .Single(behaviour => behaviour.GetType().Name == "LanternTimer");
    }

    void PickUp()
    {
        var pickup = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .First(behaviour => behaviour.GetType().Name == "LanternPickup");
        pickup.SendMessage("OnTriggerEnter2D", player.GetComponent<CircleCollider2D>());
    }

    IEnumerator WaitForLevel()
    {
        float deadline = Time.realtimeSinceStartup + 15f;
        while (Menu.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Menu.IsLoading, Is.False);
        yield return null;
        yield return null;
    }

    IEnumerator Capture(string name)
    {
        string directory = Environment.GetEnvironmentVariable("LIGHTFOOTED_METER_SCREENSHOTS");
        if (string.IsNullOrEmpty(directory)) yield break;
        Directory.CreateDirectory(directory);
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, name));
        for (int i = 0; i < 4; i++) yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        CheckpointProgress.reachedCheckpoint = false;
        yield return SceneManager.LoadSceneAsync(LevelCatalog.MenuPath);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PickupsDrainRefillPauseAndExpireWithTheLight()
    {
        Assert.That(Meter, Is.Not.Null);
        Assert.That(Alpha, Is.Zero);
        PickUp();
        yield return null;
        yield return null;
        Assert.That(Status.DurationSeconds, Is.EqualTo(5f));
        Assert.That(Alpha, Is.EqualTo(1f));
        Assert.That(Fill, Is.GreaterThan(0.95f));
        Assert.That(Meter.GetComponentsInChildren<TMP_Text>(), Is.Empty);
        Assert.That(Meter.GetComponent<Image>(), Is.Null);
        Assert.That(Meter.GetComponentsInChildren<Image>().Length, Is.EqualTo(2));
        yield return Capture("meter-full.png");
        yield return new WaitForSeconds(1.1f);
        Assert.That(Fill, Is.InRange(0.65f, 0.8f));
        Menu.Pause();
        float pausedRemaining = Status.RemainingSeconds;
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(Status.RemainingSeconds, Is.EqualTo(pausedRemaining));
        Assert.That(Meter, Is.Null);
        Menu.Resume();
        yield return null;
        yield return null;
        Assert.That(Alpha, Is.EqualTo(1f));
        Assert.That(Fill, Is.InRange(0.6f, 0.8f));
        PickUp();
        yield return null;
        yield return null;
        Assert.That(Fill, Is.GreaterThan(0.95f));
        float deadline = Time.realtimeSinceStartup + 7f;
        while (Status.RemainingSeconds > 1.4f && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Status.RemainingSeconds, Is.InRange(1f, 1.4f));
        Assert.That(Fill, Is.InRange(0.2f, 0.3f));
        Assert.That(timer.gameObject.activeSelf, Is.True);
        Assert.That(timer.GetComponents<Behaviour>().Single(component => component.GetType().Name == "Light2D").enabled, Is.True);
        Assert.That(Meter.transform.Find("Time bar/Time fill").GetComponent<Image>().color.r, Is.GreaterThan(0.9f));
        yield return Capture("meter-low.png");
        while (timer.gameObject.activeSelf && Time.realtimeSinceStartup < deadline) yield return null;
        yield return null;
        Assert.That(timer.gameObject.activeSelf, Is.False);
        Assert.That(Status.RemainingSeconds, Is.Zero);
        Assert.That(Alpha, Is.Zero);
        var tracker = player.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "PlayerLightTracker");
        Assert.That((bool)tracker.GetType().GetField("hasLantern").GetValue(tracker), Is.False);
    }

    [UnityTest]
    public IEnumerator DeathRetryAndSceneChangesDoNotKeepAnOldMeter()
    {
        PickUp();
        yield return null;
        yield return null;
        Assert.That(Alpha, Is.EqualTo(1f));
        GameMenuController.NotifyGameOver();
        yield return null;
        Assert.That(Meter, Is.Null);
        Assert.That(Menu.Restart(), Is.True);
        yield return WaitForLevel();
        FindPlayer();
        Assert.That(Status.RemainingSeconds, Is.Zero);
        Assert.That(Alpha, Is.Zero);
        PickUp();
        yield return null;
        yield return null;
        Assert.That(Alpha, Is.EqualTo(1f));
        Assert.That(Menu.LoadLevel("Assets/Scenes/TutorialLevel.unity"), Is.True);
        yield return WaitForLevel();
        FindPlayer();
        Assert.That(Alpha, Is.Zero);
        timer.gameObject.SetActive(true);
        yield return null;
        yield return null;
        Assert.That(Alpha, Is.EqualTo(1f));
        GameMenuController.NotifyVictory();
        yield return null;
        Assert.That(Meter, Is.Null);
        Assert.That(Menu.GoToMenu(), Is.True);
        yield return WaitForLevel();
        Assert.That(Meter, Is.Null);
    }

    [UnityTest]
    public IEnumerator MeterFollowsPlayerStaysOnScreenAndDoesNotTakeInput()
    {
        PickUp();
        yield return null;
        yield return null;
        var camera = Camera.main;
        foreach (var behaviour in camera.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
        var group = Meter.GetComponent<CanvasGroup>();
        Assert.That(group.interactable, Is.False);
        Assert.That(group.blocksRaycasts, Is.False);
        Assert.That(Meter.GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget), Is.True);
        Assert.That(Meter.GetComponentsInChildren<Selectable>(), Is.Empty);
        var rect = Meter.GetComponent<RectTransform>();
        float previousX = rect.anchoredPosition.x;
        player.transform.position += Vector3.right;
        yield return null;
        yield return null;
        Assert.That(rect.anchoredPosition.x, Is.GreaterThan(previousX));

        var depth = camera.WorldToViewportPoint(player.transform.position).z;
        player.transform.position = camera.ViewportToWorldPoint(new Vector3(0.99f, 0.99f, depth));
        yield return null;
        yield return null;
        Assert.That(Alpha, Is.EqualTo(1f));
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var canvasCorners = new Vector3[4];
        Meter.GetComponentInParent<Canvas>().GetComponent<RectTransform>().GetWorldCorners(canvasCorners);
        Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(canvasCorners[0].x));
        Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(canvasCorners[0].y));
        Assert.That(corners[2].x, Is.LessThanOrEqualTo(canvasCorners[2].x));
        Assert.That(corners[2].y, Is.LessThanOrEqualTo(canvasCorners[2].y));
        player.transform.position = camera.ViewportToWorldPoint(new Vector3(1.1f, 0.5f, depth));
        yield return null;
        yield return null;
        Assert.That(Alpha, Is.Zero);
    }
}
