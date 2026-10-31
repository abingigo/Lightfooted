using System.Collections;
using System.Linq;
using LightPlatformer.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class CheckpointMenuTests
{
    const string Tutorial = "Assets/Scenes/TutorialLevel.unity";
    const string NextLevel = "Assets/Scenes/Level 1.unity";
    GameMenuController Menu => GameMenuController.Instance;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        CheckpointProgress.reachedCheckpoint = false;
        yield return SceneManager.LoadSceneAsync(LevelCatalog.MenuPath);
        yield return null;
        Assert.That(Menu, Is.Not.Null);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        CheckpointProgress.reachedCheckpoint = false;
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(LevelCatalog.MenuPath);
        yield return null;
    }

    Button FindButton(string label) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
        .SingleOrDefault(button => button.GetComponentInChildren<TMP_Text>()?.text == label);

    IEnumerator WaitForLevel()
    {
        float deadline = Time.realtimeSinceStartup + 15f;
        while (Menu.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Menu.IsLoading, Is.False, "Scene load timed out");
        yield return null;
    }

    GameObject FreezePlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        Assert.That(player, Is.Not.Null);
        foreach (var behaviour in player.GetComponents<MonoBehaviour>())
            if (behaviour.GetType().Name == "PlayerController") behaviour.enabled = false;
        player.GetComponent<Rigidbody2D>().simulated = false;
        return player;
    }

    MonoBehaviour ReachTutorialCheckpoint(GameObject player)
    {
        var checkpoint = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .First(behaviour => behaviour.GetType().Name == "Checkpoint");
        var collider = player.GetComponent<CircleCollider2D>();
        Assert.That(collider, Is.Not.Null);
        checkpoint.SendMessage("OnTriggerEnter2D", collider);
        Assert.That(CheckpointProgress.HasCheckpointForScene(Tutorial), Is.True);
        return checkpoint;
    }

    IEnumerator FallToDeath(GameObject player)
    {
        var fallingDeath = player.GetComponents<MonoBehaviour>()
            .Single(behaviour => behaviour.GetType().Name == "GameEndText");
        float endY = (float)fallingDeath.GetType().GetField("EndY").GetValue(fallingDeath);
        player.transform.position = new Vector3(player.transform.position.x, endY - 5f, 0f);
        float deadline = Time.realtimeSinceStartup + 2f;
        while (Menu.State == MenuState.Playing && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Menu.State, Is.EqualTo(MenuState.GameOver));
    }

    [UnityTest]
    public IEnumerator NextLevelDeathRejectsPreviousCheckpointAndAcceptsNewCheckpoint()
    {
        Assert.That(Menu.LoadLevel(Tutorial), Is.True);
        yield return WaitForLevel();
        var checkpointType = ReachTutorialCheckpoint(FreezePlayer()).GetType();
        GameMenuController.NotifyVictory();
        yield return null;
        FindButton("Next level").onClick.Invoke();
        yield return WaitForLevel();
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(NextLevel));
        var nextPlayer = FreezePlayer();

        // Keep the tutorial checkpoint when moving to Level 1.
        Assert.That(CheckpointProgress.reachedCheckpoint, Is.True);
        Assert.That(CheckpointProgress.saveScene, Is.EqualTo(Tutorial));
        yield return FallToDeath(nextPlayer);
        Assert.That(FindButton("Restart level"), Is.Not.Null);
        Assert.That(FindButton("Respawn at checkpoint"), Is.Null);
        Assert.That(FindButton("Restart from beginning"), Is.Null);
        FindButton("Restart level").onClick.Invoke();
        yield return WaitForLevel();
        var player = FreezePlayer();
        Assert.That(CheckpointProgress.HasCheckpointForScene(NextLevel), Is.False);

        // Level 1 has no checkpoint, so add one for this check.
        var newCheckpoint = new GameObject("Test checkpoint");
        newCheckpoint.transform.position = player.transform.position + Vector3.right * 2f;
        var checkpoint = newCheckpoint.AddComponent(checkpointType);
        checkpoint.SendMessage("OnTriggerEnter2D", player.GetComponent<CircleCollider2D>());
        var savedPosition = CheckpointProgress.savePos;
        Assert.That(CheckpointProgress.HasCheckpointForScene(NextLevel), Is.True);
        yield return FallToDeath(player);
        Assert.That(FindButton("Respawn at checkpoint"), Is.Not.Null);
        Assert.That(FindButton("Restart from beginning"), Is.Not.Null);
        FindButton("Respawn at checkpoint").onClick.Invoke();
        yield return WaitForLevel();
        player = FreezePlayer();
        Assert.That(Vector3.Distance(player.transform.position, savedPosition), Is.LessThan(0.2f));
    }

    [UnityTest]
    public IEnumerator FreshDeathAndRestartFromBeginningOfferLevelRestart()
    {
        Assert.That(Menu.LoadLevel(Tutorial), Is.True);
        yield return WaitForLevel();
        FreezePlayer();
        GameMenuController.NotifyGameOver();
        yield return null;
        Assert.That(FindButton("Restart level"), Is.Not.Null);
        Assert.That(FindButton("Respawn at checkpoint"), Is.Null);
        FindButton("Restart level").onClick.Invoke();
        yield return WaitForLevel();
        ReachTutorialCheckpoint(FreezePlayer());
        GameMenuController.NotifyGameOver();
        yield return null;
        FindButton("Restart from beginning").onClick.Invoke();
        yield return WaitForLevel();
        FreezePlayer();
        Assert.That(CheckpointProgress.reachedCheckpoint, Is.False);
        GameMenuController.NotifyGameOver();
        yield return null;
        Assert.That(FindButton("Restart level"), Is.Not.Null);
        Assert.That(FindButton("Respawn at checkpoint"), Is.Null);
        Assert.That(FindButton("Restart from beginning"), Is.Null);
    }
}
