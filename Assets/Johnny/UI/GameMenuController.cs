using System;
using Core;
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
        public static bool GameplayInputBlocked => Game is { } game && !game.IsGameplayActive;
        public MenuState State => Map(Game?.State ?? GameState.Loading);
        public bool IsLoading => State == MenuState.Loading;
        public string ErrorMessage { get; private set; }
        internal LevelScoreboard.Result VictoryScore  {get; private set; }
        public event Action ViewChanged;
        public event Action<float> LoadingProgress;
        GameMenuView view;
        IGameService boundGame;

        static IGameService Game => ServiceRegistry.TryGet(out IGameService game) ? game : null;

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

        static MenuState Map(GameState state) => state switch
        {
            GameState.Menu => MenuState.LevelSelect,
            GameState.Playing => MenuState.Playing,
            GameState.Paused => MenuState.Paused,
            GameState.GameOver => MenuState.GameOver,
            GameState.Victory => MenuState.Victory,
            _ => MenuState.Loading
        };

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            view = gameObject.AddComponent<GameMenuView>();
            SceneManager.sceneLoaded += SceneLoaded;
            WebAnalytics.SetConsent(true);
            Bind();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            Unbind();
            if (Instance == this) Instance = null;
        }

        void Bind()
        {
            var game = Game;
            if (ReferenceEquals(game, boundGame)) return;
            Unbind();
            boundGame = game;
            if (game == null) return;
            game.StateChanged += GameStateChanged;
            game.DeathReported += RecordDeath;
            game.GoalReached += RecordVictory;
            game.LoadingProgress += ForwardLoadingProgress;
        }

        void Unbind()
        {
            if (boundGame == null) return;
            boundGame.StateChanged -= GameStateChanged;
            boundGame.DeathReported -= RecordDeath;
            boundGame.GoalReached -= RecordVictory;
            boundGame.LoadingProgress -= ForwardLoadingProgress;
            boundGame = null;
        }

        void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            Debug.Log("Game UI ready: " + scene.path);
            ErrorMessage = null;
            EnsureEventSystem();
            Bind();
            Refresh();
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
            Bind();
            bool escape = Input.GetKeyDown(KeyCode.Escape);
            if (escape && view.CloseControls()) return;
            if (escape || Input.GetKeyDown(KeyCode.P))
            {
                if (State == MenuState.Playing) Pause();
                else if (State == MenuState.Paused) Resume();
            }
            if (Input.GetKeyDown(KeyCode.Tab)) view.MoveFocus(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }

        void GameStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.Loading)
            {
                ErrorMessage = null;
                LevelStats.StartAttempt();
            }
            Refresh();
        }

        void ForwardLoadingProgress(float progress) => LoadingProgress?.Invoke(progress);

        void Refresh()
        {
            view.Render(this);
            ViewChanged?.Invoke();
        }

        public void Pause()
        {
            Bind();
            Game?.Pause();
        }

        public void Resume()
        {
            Bind();
            Game?.Resume();
        }

        public static void NotifyGameOver(string cause = "unknown") => Game?.ReportDeath(cause);

        public static void NotifyVictory() => Game?.ReportGoalReached();

        void RecordDeath(string cause)
        {
            string level = SceneManager.GetActiveScene().name;
            LevelStats.RecordDeath(level);

            PlayerDeathContext ctx = PlayerDeathContext.Current;
            if (ctx != null)
                WebAnalytics.ReportDeath(level, cause, ctx.LastGroundedPosition, ctx.transform.position);
        }

        void RecordVictory()
            {
                var scene = SceneManager.GetActiveScene();
                    float seconds = LevelStats.AttemptSeconds;
                int deaths = LevelStats.DeathsIn(scene.name);

                VictoryScore = LevelScoreboard.Record(
                    scene.path, seconds,deaths, LevelStats.CoinsCollected, LevelStats.CoinTotal);

                WebAnalytics.ReportCompletion(scene.name, seconds, LevelStats.CoinStatus(scene.name), deaths);
                LevelStats.ClearLevel(scene.name);
            }

        public bool Restart()
        {
            if (State == MenuState.LevelSelect || IsLoading) return false;
            return Request(SceneManager.GetActiveScene().path, game => game.Retry());
        }

        public bool RestartFromBeginning()
        {
            if (State == MenuState.LevelSelect || IsLoading) return false;
            return Request(SceneManager.GetActiveScene().path, game => game.RestartLevel());
        }

        public bool LoadLevel(string path)
        {
            if (IsLoading) return false;
            return Request(path, game => game.LoadLevel(path));
        }

        bool Request(string path, Func<IGameService, bool> load)
        {
            var level = LevelCatalog.ReadBuildLevels().Find(entry => entry.ScenePath == path);
            if (level == null || !Application.CanStreamedLevelBeLoaded(path))
            {
                ShowError("This level is unavailable. Choose another level or return to level select.");
                return false;
            }
            Bind();
            if (Game == null || !load(Game))
            {
                ShowError("The level could not be opened. Please try again.");
                return false;
            }
            return true;
        }

        public bool GoToMenu()
        {
            if (IsLoading) return false;
            Bind();
            if (Game == null || !Game.GoToMenu())
            {
                ShowError("The level select could not be opened. Please try again.");
                return false;
            }
            return true;
        }

        public bool NextLevel()
        {
            if (State != MenuState.Victory) return false;
            Bind();
            return Game != null && Game.NextLevel();
        }

        void ShowError(string message)
        {
            ErrorMessage = message;
            view.ShowError(message);
        }
    }
}
