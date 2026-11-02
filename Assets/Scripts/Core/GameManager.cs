using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public sealed class GameManager : Service<IGameService>, IGameService
    {
        [Tooltip("Levels in play order. The level select and Next Level read this.")] [SerializeField]
        private LevelList levels;

        private readonly List<string> playable = new();
        private bool inLevel;
        private bool loading;
        private int resumeFrame;

        public GameState State { get; private set; } = GameState.Loading;
        public bool IsGameplayActive => State == GameState.Playing && Time.frameCount > resumeFrame;
        public Scene CurrentLevel { get; private set; }
        public LevelList Levels => levels;

        public event Action<GameState, GameState> StateChanged;
        public event Action<Scene> LevelStarted;
        public event Action<Scene> LevelEnded;
        public event Action<string> DeathReported;
        public event Action GoalReached;
        public event Action<float> LoadingProgress;

        private string MenuPath =>
            levels != null && !string.IsNullOrEmpty(levels.MenuPath)
                ? levels.MenuPath
                : SceneUtility.GetScenePathByBuildIndex(0);

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (State == GameState.Playing && Time.timeScale == 0f) Pause();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFirstLevel()
        {
            if (ServiceRegistry.TryGet(out IGameService service) && service is GameManager manager)
                manager.StartLevel(SceneManager.GetActiveScene());
        }

        public bool LoadLevel(string scenePath)
        {
            if (loading || string.IsNullOrEmpty(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath))
                return false;
            StartCoroutine(Load(scenePath));
            return true;
        }

        public bool NextLevel()
        {
            if (State != GameState.Victory) return false;
            var levelPaths = PlayableLevels();
            int current = levelPaths.IndexOf(SceneManager.GetActiveScene().path);
            return current >= 0 && current + 1 < levelPaths.Count && LoadLevel(levelPaths[current + 1]);
        }

        public bool GoToMenu() => LoadLevel(MenuPath);

        public bool Retry() =>
            State != GameState.Menu && State != GameState.Loading && LoadLevel(SceneManager.GetActiveScene().path);

        public bool RestartLevel() => Retry();

        public bool Pause()
        {
            if (State != GameState.Playing) return false;
            SetState(GameState.Paused);
            return true;
        }

        public bool Resume()
        {
            if (State != GameState.Paused) return false;
            SetState(GameState.Playing);
            return true;
        }

        public bool ReportDeath(string cause)
        {
            if (State != GameState.Playing) return false;
            DeathReported?.Invoke(cause);
            SetState(GameState.GameOver);
            return true;
        }

        public bool ReportGoalReached()
        {
            if (State != GameState.Playing) return false;
            GoalReached?.Invoke();
            SetState(GameState.Victory);
            return true;
        }

        private List<string> PlayableLevels()
        {
            playable.Clear();
            if (levels != null && levels.Levels.Count > 0)
            {
                foreach (var level in levels.Levels)
                    if (level.IsPlayable && !playable.Contains(level.ScenePath)) playable.Add(level.ScenePath);
                return playable;
            }

            string menu = MenuPath;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path != menu) playable.Add(path);
            }

            return playable;
        }

        private IEnumerator Load(string scenePath)
        {
            loading = true;
            SetState(GameState.Loading);
            if (inLevel) EndLevel();

            var operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"GameManager: could not load '{scenePath}'.", this);
                loading = false;
                StartLevel(SceneManager.GetActiveScene());
                yield break;
            }

            while (!operation.isDone)
            {
                LoadingProgress?.Invoke(Mathf.Clamp01(operation.progress / 0.9f));
                yield return null;
            }

            loading = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) StartLevel(scene);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (inLevel && scene == CurrentLevel) EndLevel();
        }

        private void StartLevel(Scene scene)
        {
            bool fresh = !inLevel || scene != CurrentLevel;
            if (fresh)
            {
                if (inLevel) EndLevel();
                CurrentLevel = scene;
                inLevel = true;
                LevelStarted?.Invoke(scene);
            }

            SetState(scene.path == MenuPath ? GameState.Menu : GameState.Playing, true);
        }

        private void EndLevel()
        {
            var ended = CurrentLevel;
            CurrentLevel = default;
            inLevel = false;
            LevelEnded?.Invoke(ended);
        }

        private void SetState(GameState next, bool reapply = false)
        {
            var previous = State;
            if (previous == next && !reapply) return;

            State = next;
            if (next == GameState.Playing) resumeFrame = Time.frameCount;
            Time.timeScale = next is GameState.Paused or GameState.GameOver or GameState.Victory ? 0f : 1f;
            if (previous != next) StateChanged?.Invoke(previous, next);
        }
    }
}
