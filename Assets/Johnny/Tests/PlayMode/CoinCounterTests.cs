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

public class CoinCounterTests
{
    GameMenuController Menu => GameMenuController.Instance;
    CoinCounter Counter => Object.FindFirstObjectByType<CoinCounter>();
    TMP_Text Label => Counter.GetComponent<TMP_Text>();
    MonoBehaviour[] Coins() => Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
        .Where(component => component.GetType().Name == "Coin").ToArray();
    GameObject player;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        CheckpointProgress.reachedCheckpoint = false;
        yield return SceneManager.LoadSceneAsync(LevelCatalog.MenuPath);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        CheckpointProgress.reachedCheckpoint = false;
        yield return SceneManager.LoadSceneAsync(LevelCatalog.MenuPath);
        yield return null;
    }

    IEnumerator WaitForLevel()
    {
        float deadline = Time.realtimeSinceStartup + 15f;
        while (Menu.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Menu.IsLoading, Is.False);
        yield return null;
        yield return null;
        player = GameObject.FindGameObjectWithTag("Player");
        foreach (var behaviour in player.GetComponents<MonoBehaviour>())
            if (behaviour.GetType().Name == "PlayerController") behaviour.enabled = false;
        player.GetComponent<Rigidbody2D>().simulated = false;
    }

    IEnumerator Load(string path)
    {
        Assert.That(Menu.LoadLevel(path), Is.True);
        yield return WaitForLevel();
    }

    void PickUp(MonoBehaviour coin) => coin.SendMessage("OnTriggerEnter2D", player.GetComponent<CircleCollider2D>());

    IEnumerator Capture(string name)
    {
        string directory = Environment.GetEnvironmentVariable("LIGHTFOOTED_COIN_SCREENSHOTS");
        if (string.IsNullOrEmpty(directory)) yield break;
        Directory.CreateDirectory(directory);
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, name));
        for (int i = 0; i < 4; i++) yield return null;
    }

    [UnityTest]
    public IEnumerator RealCoinUpdatesDisplayAndKeepsPickupMessageAndAnalytics()
    {
        yield return Load("Assets/Scenes/Level 2.unity");
        var coin = Coins().Single();
        Assert.That(Label.enabled, Is.True);
        Assert.That(Label.text, Is.EqualTo("Coins 0/1"));
        Assert.That(LevelStats.CoinStatus("Level 2"), Is.EqualTo("no"));
        Assert.That(Label.raycastTarget, Is.False);
        Assert.That(Counter.GetComponentsInChildren<Selectable>(), Is.Empty);
        var corners = new Vector3[4];
        Counter.GetComponent<RectTransform>().GetWorldCorners(corners);
        var barCorners = new Vector3[4];
        ((RectTransform)Counter.transform.parent).GetWorldCorners(barCorners);
        Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(barCorners[0].x));
        Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(barCorners[0].y));
        Assert.That(corners[2].x, Is.LessThanOrEqualTo(barCorners[2].x));
        Assert.That(corners[2].y, Is.LessThanOrEqualTo(barCorners[2].y));
        yield return Capture("coins-before.png");

        PickUp(coin);
        Assert.That(Label.text, Is.EqualTo("Coins 1/1"), "The counter should update during pickup.");
        Assert.That(LevelStats.CoinsCollected, Is.EqualTo(1));
        Assert.That(LevelStats.CoinTotal, Is.EqualTo(1));
        Assert.That(LevelStats.CoinStatus("Level 2"), Is.EqualTo("yes"));
        var message = (TMP_Text)coin.GetType().GetField("text").GetValue(coin);
        Assert.That(message.gameObject.activeInHierarchy, Is.True);
        Assert.That(message.text, Is.EqualTo("Coin collected!"));
        yield return null;
        yield return Capture("coins-after.png");
    }

    [UnityTest]
    public IEnumerator TwoCoinsCountOnceAndKeepTheirTotalAfterRemoval()
    {
        yield return Load("Assets/Scenes/Level 2.unity");
        var first = Coins().Single();
        var secondObject = Object.Instantiate(first.gameObject, first.transform.position + Vector3.right, Quaternion.identity);
        var second = secondObject.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "Coin");
        yield return null;
        Assert.That(Label.text, Is.EqualTo("Coins 0/2"));
        PickUp(first);
        PickUp(first);
        Assert.That(Label.text, Is.EqualTo("Coins 1/2"));
        Menu.Pause();
        yield return null;
        Assert.That(Counter, Is.Null);
        Assert.That(LevelStats.CoinsCollected, Is.EqualTo(1));
        Menu.Resume();
        yield return null;
        Assert.That(Label.text, Is.EqualTo("Coins 1/2"));
        float deadline = Time.realtimeSinceStartup + 5f;
        while (first != null && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(first == null, Is.True);
        Assert.That(Label.text, Is.EqualTo("Coins 1/2"));
        PickUp(second);
        PickUp(second);
        Assert.That(Label.text, Is.EqualTo("Coins 2/2"));
        deadline = Time.realtimeSinceStartup + 5f;
        while (second != null && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(second == null, Is.True);
        Assert.That(Coins(), Is.Empty);
        Assert.That(LevelStats.CoinTotal, Is.EqualTo(2));
        Assert.That(Label.text, Is.EqualTo("Coins 2/2"));
    }

    [UnityTest]
    public IEnumerator CheckpointRetryAndLevelChangesResetProgress()
    {
        yield return Load("Assets/Scenes/Level 3.unity");
        var checkpoint = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .First(component => component.GetType().Name == "Checkpoint");
        checkpoint.SendMessage("OnTriggerEnter2D", player.GetComponent<CircleCollider2D>());
        Assert.That(CheckpointProgress.HasCheckpointForScene("Assets/Scenes/Level 3.unity"), Is.True);
        PickUp(Coins().Single());
        Assert.That(Label.text, Is.EqualTo("Coins 1/1"));
        GameMenuController.NotifyGameOver();
        yield return null;
        Assert.That(Counter, Is.Null);
        Assert.That(Menu.Restart(), Is.True);
        yield return WaitForLevel();
        Assert.That(CheckpointProgress.reachedCheckpoint, Is.True);
        Assert.That(Label.text, Is.EqualTo("Coins 0/1"));
        Assert.That(LevelStats.CoinCollected, Is.False);
        Assert.That(Coins().Single().GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True);
        PickUp(Coins().Single());
        Menu.Pause();
        Assert.That(Menu.Restart(), Is.True);
        yield return WaitForLevel();
        Assert.That(Label.text, Is.EqualTo("Coins 0/1"));
        PickUp(Coins().Single());
        yield return Load("Assets/Scenes/Level 2.unity");
        Assert.That(Label.text, Is.EqualTo("Coins 0/1"));
        PickUp(Coins().Single());

        yield return SceneManager.LoadSceneAsync("Assets/Scenes/TutorialLevel.unity");
        yield return null;
        yield return null;
        Assert.That(LevelStats.CoinTotal, Is.Zero);
        Assert.That(LevelStats.CoinsCollected, Is.Zero);
        Assert.That(Label.enabled, Is.False);
        Assert.That(Label.text, Is.Empty);
        yield return Load("Assets/Scenes/Level 1.unity");
        Assert.That(Label.enabled, Is.False);
        Assert.That(Menu.GoToMenu(), Is.True);
        float deadline = Time.realtimeSinceStartup + 15f;
        while (Menu.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        yield return null;
        Assert.That(Counter, Is.Null);
    }
}
