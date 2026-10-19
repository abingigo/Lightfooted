using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LightPlatformer.UI
{
    public enum MenuState { Playing, Paused, GameOver, Victory, LevelSelect, Loading }

    [DefaultExecutionOrder(-1000)]
    public sealed class GameMenuController : MonoBehaviour
    {
        public static GameMenuController Instance { get; private set; }
        public static bool GameplayInputBlocked => Instance != null && (Instance.State != MenuState.Playing || Time.frameCount <= Instance.inputResumeFrame);
        public MenuState State { get; private set; } = MenuState.Loading;
        public bool IsLoading => State == MenuState.Loading;
        public string ErrorMessage { get; private set; }
        public event Action ViewChanged;
        public event Action<float> LoadingProgress;
        GameMenuView view;
        int inputResumeFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance == null) new GameObject("Game menus").AddComponent<GameMenuController>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ShowInitialScene()
        {
            // Initial scene callbacks can precede Awake on objects created before scene load.
            // Explicit initialization also covers starting directly in a gameplay scene.
            if (Instance == null) Bootstrap();
            Instance.SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            view = gameObject.AddComponent<GameMenuView>();
            SceneManager.sceneLoaded += SceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (Instance == this) { Instance = null; Time.timeScale = 1f; }
        }

        void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            Debug.Log("Game UI ready: " + scene.path);
            Time.timeScale = 1f;
            ErrorMessage = null;
            EnsureEventSystem();
            SetState(scene.path == LevelCatalog.MenuPath ? MenuState.LevelSelect : MenuState.Playing);
        }

        void EnsureEventSystem()
        {
            var systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (systems.Length == 0)
                new GameObject("Menu EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            // Each scene owns its EventSystem to avoid duplicates after loading.
        }

        void Update()
        {
            bool escape = Input.GetKeyDown(KeyCode.Escape);
            if (escape && view.CloseControls()) return;
            if (escape || Input.GetKeyDown(KeyCode.P))
            {
                if (State == MenuState.Playing) Pause();
                else if (State == MenuState.Paused) Resume();
            }
            if (Input.GetKeyDown(KeyCode.Tab)) view.MoveFocus(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            if (State == MenuState.Playing && Time.timeScale == 0f) Pause();
        }

        void SetState(MenuState state)
        {
            State = state;
            if (state == MenuState.Playing) inputResumeFrame = Time.frameCount;
            view.Render(this);
            ViewChanged?.Invoke();
        }

        public void Pause()
        {
            if (State != MenuState.Playing) return;
            Time.timeScale = 0f;
            SetState(MenuState.Paused);
        }

        public void Resume()
        {
            if (State != MenuState.Paused) return;
            Time.timeScale = 1f;
            SetState(MenuState.Playing);
        }

        public static void NotifyGameOver()
        {
            if (Instance != null) Instance.Finish(MenuState.GameOver);
        }

        public static void NotifyVictory()
        {
            if (Instance != null) Instance.Finish(MenuState.Victory);
        }

        void Finish(MenuState result)
        {
            if (State != MenuState.Playing) return;

            if (result == MenuState.GameOver)
            {
                CheckpointProgress.deathTimes++;//death time counter here
            }
            Time.timeScale = 0f;
            SetState(result);
        }

        public bool Restart()
        {
            if (State == MenuState.LevelSelect || IsLoading) return false;
            return LoadLevel(SceneManager.GetActiveScene().path);
        }

        public bool RestartFromBeginning()
        {
            CheckpointProgress.reachedCheckpoint = false;
            return Restart();
        }
        public bool LoadLevel(string path)
        {
            if (IsLoading) return false;
            var level = LevelCatalog.ReadBuildLevels().Find(entry => entry.ScenePath == path);
            if (level == null || !Application.CanStreamedLevelBeLoaded(path))
            {
                ErrorMessage = "This level is unavailable. Choose another level or return to level select.";
                view.ShowError(ErrorMessage);
                return false;
            }
            StartCoroutine(Load(path));
            return true;
        }

        public bool GoToMenu()
        {
            if (IsLoading) return false;
            if (!Application.CanStreamedLevelBeLoaded(LevelCatalog.MenuPath))
            {
                // Useful when testing a level directly or in a build with no menu scene.
                Time.timeScale = 0f;
                SetState(MenuState.LevelSelect);
                return true;
            }
            StartCoroutine(Load(LevelCatalog.MenuPath));
            return true;
        }

        public bool NextLevel()
        {
            if (State != MenuState.Victory) return false;
            var levels = LevelCatalog.ReadBuildLevels();
            int current = levels.FindIndex(level => level.ScenePath == SceneManager.GetActiveScene().path);
            return current >= 0 && current + 1 < levels.Count && LoadLevel(levels[current + 1].ScenePath);
        }

        IEnumerator Load(string path)
        {
            ErrorMessage = null;
            SetState(MenuState.Loading);
            // Unpause before loading so the restarted level has a running clock.
            Time.timeScale = 1f;
            AsyncOperation operation;
            try { operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single); }
            catch (Exception)
            {
                Time.timeScale = 0f;
                SetState(MenuState.LevelSelect);
                ErrorMessage = "The level could not be opened. Please try again.";
                view.ShowError(ErrorMessage);
                yield break;
            }
            if (operation == null)
            {
                SetState(MenuState.LevelSelect);
                ErrorMessage = "The level could not be opened. Please try again.";
                view.ShowError(ErrorMessage);
                yield break;
            }
            while (!operation.isDone)
            {
                LoadingProgress?.Invoke(Mathf.Clamp01(operation.progress / 0.9f));
                yield return null;
            }
        }
    }
}
