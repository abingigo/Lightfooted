using System.Collections;
using System.Linq;
using LightPlatformer.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class GameMenuTests
{
    const string Tutorial = "Assets/Scenes/TutorialLevel.unity";
    GameMenuController Menu => GameMenuController.Instance;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1;
        Assert.That(Menu, Is.Not.Null, "Runtime bootstrap must create the UI without a scene component");
        yield return SceneManager.LoadSceneAsync(LevelCatalog.MenuPath);
        yield return null;
        Assert.That(Menu.State, Is.EqualTo(MenuState.LevelSelect));
    }

    Button FindButton(string text) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
        .Single(button => button.GetComponentInChildren<TMP_Text>()?.text == text);

    IEnumerator WaitForLevel()
    {
        float timeout = Time.realtimeSinceStartup + 15;
        while (Menu.IsLoading && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(Menu.IsLoading, Is.False, "Scene load timed out");
        yield return null;
    }

    [Test]
    public void CatalogExcludesMenuEmptyPathsAndDuplicates()
    {
        var levels = LevelCatalog.FromPaths(new[] { LevelCatalog.MenuPath, Tutorial, Tutorial, "", "Assets/Tests/Test.unity" });
        Assert.That(levels.Count, Is.EqualTo(1));
        Assert.That(levels[0].Title, Is.EqualTo("Tutorial"));
        Assert.That(levels[0].ScenePath, Is.EqualTo(Tutorial));
    }

    [UnityTest]
    public IEnumerator TutorialIsAvailableAndPlayWorks()
    {
        Assert.That(LevelCatalog.ReadBuildLevels().Count(level => level.ScenePath == Tutorial), Is.EqualTo(1));
        FindButton("Play Tutorial").onClick.Invoke();
        yield return WaitForLevel();
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Tutorial));
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing));
        Assert.That(Time.timeScale, Is.EqualTo(1));
        Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That(FindButton("Pause  [Esc]").interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator EmptyCatalogDisablesPlayAndKeepsControlsUsable()
    {
        Menu.GetComponent<GameMenuView>().Render(Menu, new LevelEntry[0]);
        yield return null;
        var play = FindButton("No level available");
        Assert.That(play.interactable, Is.False);
        Assert.That(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(text => text.text == "No levels yet"), Is.True);
        FindButton("Controls").onClick.Invoke();
        yield return null;
        Assert.That(FindButton("Back").interactable, Is.True);
        FindButton("Back").onClick.Invoke();
        yield return null;
        Assert.That(FindButton("No level available").interactable, Is.False);
        Assert.That(Menu.State, Is.EqualTo(MenuState.LevelSelect));
    }

    [UnityTest]
    public IEnumerator OneLevelCatalogOffersPlayableTutorialAndUpdatedControls()
    {
        Menu.GetComponent<GameMenuView>().Render(Menu, new[] { new LevelEntry(Tutorial) });
        yield return null;
        Assert.That(FindButton("Play Tutorial").interactable, Is.True);
        Assert.That(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(text => text.text == "1 level available"), Is.True);
        FindButton("Controls").onClick.Invoke();
        yield return null;
        var instructions = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Single(text => text.text.Contains("Click a light, then F"));
        Assert.That(instructions.text, Does.Contain("E / Q").And.Contain("R "));
        Assert.That(instructions.isTextOverflowing, Is.False, "The extra team controls must fit the panel");
        FindButton("Back").onClick.Invoke();
        yield return null;
        FindButton("Play Tutorial").onClick.Invoke();
        yield return WaitForLevel();
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Tutorial));
    }

    [UnityTest]
    public IEnumerator GameplayHudDoesNotCaptureJumpSubmitAfterLoadOrResume()
    {
        Menu.LoadLevel(Tutorial);
        yield return WaitForLevel();
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
        Menu.GetComponent<GameMenuView>().MoveFocus(false);
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
        Assert.That(FindButton("Pause  [Esc]").navigation.mode, Is.EqualTo(Navigation.Mode.None));
        Menu.Pause(); yield return null;
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null);
        FindButton("Resume game").onClick.Invoke(); yield return null;
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing));
    }

    [UnityTest]
    public IEnumerator PauseResumeAndRestartUseActualButtons()
    {
        FindButton("Play Tutorial").onClick.Invoke();
        yield return WaitForLevel();
        FindButton("Pause  [Esc]").onClick.Invoke();
        yield return null;
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(GameMenuController.GameplayInputBlocked, Is.True);
        FindButton("Resume game").onClick.Invoke();
        yield return null;
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing));
        Assert.That(Time.timeScale, Is.EqualTo(1));
        Menu.Pause();
        yield return null;
        FindButton("Restart level").onClick.Invoke();
        Assert.That(Menu.Restart(), Is.False, "Repeated restart must not start another load");
        yield return WaitForLevel();
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing));
        Assert.That(Time.timeScale, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator DeathCannotResumeAndCanRestart()
    {
        Menu.LoadLevel(Tutorial);
        yield return WaitForLevel();
        GameMenuController.NotifyGameOver();
        yield return null;
        Assert.That(Menu.State, Is.EqualTo(MenuState.GameOver));
        Menu.Resume();
        Assert.That(Menu.State, Is.EqualTo(MenuState.GameOver));
        FindButton("Restart level").onClick.Invoke();
        yield return WaitForLevel();
        Assert.That(Menu.State, Is.EqualTo(MenuState.Playing));
        Assert.That(Time.timeScale, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator FinalLevelVictoryHasNoNextAndMenuWorks()
    {
        Menu.LoadLevel(LevelCatalog.ReadBuildLevels().Last().ScenePath);
        yield return WaitForLevel();
        GameMenuController.NotifyVictory();
        yield return null;
        Assert.That(Menu.State, Is.EqualTo(MenuState.Victory));
        Assert.That(Menu.NextLevel(), Is.False);
        Assert.That(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(button => button.GetComponentInChildren<TMP_Text>()?.text == "Next level"), Is.False);
        FindButton("Level select").onClick.Invoke();
        yield return WaitForLevel();
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(LevelCatalog.MenuPath));
        Assert.That(Menu.State, Is.EqualTo(MenuState.LevelSelect));
        Assert.That(FindButton("Play Tutorial").interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator InvalidLevelDoesNotStartLoadingAndShowsMessage()
    {
        Assert.That(Menu.LoadLevel("Assets/Scenes/Missing.unity"), Is.False);
        yield return null;
        Assert.That(Menu.State, Is.EqualTo(MenuState.LevelSelect));
        Assert.That(Menu.ErrorMessage, Does.Contain("unavailable"));
        Assert.That(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(text => text.text == Menu.ErrorMessage), Is.True);
    }

    [UnityTest]
    public IEnumerator KeyboardFocusSkipsDisabledPlayAndCanOpenControls()
    {
        Menu.GetComponent<GameMenuView>().Render(Menu, new LevelEntry[0]);
        yield return null;
        Menu.GetComponent<GameMenuView>().MoveFocus(false);
        Assert.That(EventSystem.current.currentSelectedGameObject.GetComponent<Button>(), Is.EqualTo(FindButton("Controls")));
        ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        yield return null;
        Assert.That(EventSystem.current.currentSelectedGameObject.GetComponent<Button>(), Is.EqualTo(FindButton("Back")));
    }

    [UnityTest]
    public IEnumerator ActualFallingDeathOpensRestartScreen()
    {
        Menu.LoadLevel(Tutorial);
        yield return WaitForLevel();
        var fallingDeath = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(component => component.GetType().Name == "GameEndText");
        fallingDeath.transform.position = new Vector3(0, -25, 0);
        yield return null;
        Assert.That(Menu.State, Is.EqualTo(MenuState.GameOver));
        Assert.That(FindButton("Restart level").interactable, Is.True);
        FindButton("Level select").onClick.Invoke();
        yield return WaitForLevel();
        Assert.That(Menu.State, Is.EqualTo(MenuState.LevelSelect));
    }

    [UnityTest]
    public IEnumerator ActualTutorialVictoryOffersNextLevelAndLoadsLab()
    {
        Menu.LoadLevel(Tutorial);
        yield return WaitForLevel();
        var victory = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(component => component.GetType().Name == "Victory");
        var player = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(component => component.GetType().Name == "PlayerController");
        victory.SendMessage("OnTriggerEnter2D", player.GetComponent<BoxCollider2D>());
        yield return null;
        Assert.That(Menu.State, Is.EqualTo(MenuState.Victory));
        Assert.That(FindButton("Next level").interactable, Is.True);
        FindButton("Next level").onClick.Invoke();
        yield return WaitForLevel();
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo("Assets/Scenes/Johnny/ObstacleLab.unity"));
    }

    [UnityTest]
    public IEnumerator MenuButtonsRemainInsideCanvas()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        var uiCanvas = Menu.GetComponentInChildren<Canvas>();
        var canvasRect = (RectTransform)uiCanvas.transform;
        var bounds = new Vector3[4];
        canvasRect.GetWorldCorners(bounds);
        foreach (var button in Menu.GetComponentsInChildren<Button>())
        {
            var corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(bounds[0].x), button.name);
            Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(bounds[0].y), button.name);
            Assert.That(corners[2].x, Is.LessThanOrEqualTo(bounds[2].x), button.name);
            Assert.That(corners[2].y, Is.LessThanOrEqualTo(bounds[2].y), button.name);
        }
    }
}
