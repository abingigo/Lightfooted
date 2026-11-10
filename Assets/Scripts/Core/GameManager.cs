using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public sealed class GameManager : Service<IGameService>, IGameService
    {
        private sealed class Snapshot
        {
            public string ScenePath;
            public Vector3 RespawnPoint;
            public readonly Dictionary<string, string> States = new();
            public readonly HashSet<string> Consumed = new();
        }

        [Tooltip("Levels in play order. The level select and Next Level read this.")] [SerializeField]
        private LevelList levels;

        [Tooltip("On death, skip the Game Over screen and retry at once (from the checkpoint if one was reached).")]
        [SerializeField]
        private bool instantRespawn = true;

        private readonly List<string> playable = new();
        private readonly List<KeyValuePair<string, CheckpointState>> inventory = new();
        private readonly List<CheckpointState> found = new();
        private readonly HashSet<string> ids = new();
        private readonly Dictionary<string, int> levelDeaths = new();
        private Snapshot snapshot;
        private bool restoreOnLoad;
        private bool continueRunOnLoad;
        private bool inLevel;
        private bool loading;
        private int resumeFrame;

        public GameState State { get; private set; } = GameState.Loading;
        public bool IsGameplayActive => State == GameState.Playing && Time.frameCount > resumeFrame;
        public Scene CurrentLevel { get; private set; }
        public LevelList Levels => levels;

        public bool HasCheckpoint => snapshot != null;
        public string CheckpointScene => snapshot?.ScenePath;
        public Vector3 CheckpointPosition => snapshot?.RespawnPoint ?? Vector3.zero;
        public bool HasCheckpointFor(string scenePath) => snapshot != null && snapshot.ScenePath == scenePath;

        public int SessionDeaths { get; private set; }
        public int DeathsIn(string level) => levelDeaths.TryGetValue(level, out int count) ? count : 0;
        public float AttemptSeconds { get; private set; }

        public event Action<GameState, GameState> StateChanged;
        public event Action<Scene> LevelStarted;
        public event Action<Scene> LevelEnded;
        public event Action<string> DeathReported;
        public event Action GoalReached;
        public event Action<Vector3> Respawned;
        public event Action CheckpointChanged;
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
            if (State != GameState.Playing) return;
            if (Time.timeScale == 0f) Pause();
            else AttemptSeconds += Time.deltaTime;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFirstLevel()
        {
            if (ServiceRegistry.TryGet(out IGameService service) && service is GameManager manager)
                manager.StartLevel(SceneManager.GetActiveScene());
        }

        public bool LoadLevel(string scenePath) => StartLoad(scenePath, false);

        public bool NextLevel()
        {
            if (State != GameState.Victory) return false;
            var levelPaths = PlayableLevels();
            int current = levelPaths.IndexOf(SceneManager.GetActiveScene().path);
            return current >= 0 && current + 1 < levelPaths.Count && StartLoad(levelPaths[current + 1], false);
        }

        public bool GoToMenu() => StartLoad(MenuPath, false);

        public bool Retry() => CanReload() && StartLoad(SceneManager.GetActiveScene().path, true);

        public bool RestartLevel() => CanReload() && StartLoad(SceneManager.GetActiveScene().path, false);

        public void DiscardCheckpoint() => SetSnapshot(null);

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
            string level = SceneManager.GetActiveScene().name;
            SessionDeaths++;
            levelDeaths[level] = DeathsIn(level) + 1;
            DeathReported?.Invoke(cause);
            SetState(GameState.GameOver);
            if (instantRespawn) Retry();
            return true;
        }

        public bool ReportGoalReached()
        {
            if (State != GameState.Playing) return false;
            GoalReached?.Invoke();
            levelDeaths.Remove(SceneManager.GetActiveScene().name);
            SetSnapshot(null);
            SetState(GameState.Victory);
            return true;
        }

        public bool ReportCheckpoint(Vector3 respawnPoint)
        {
            if (State != GameState.Playing || !inLevel) return false;
            if (HasCheckpointFor(CurrentLevel.path) && (snapshot.RespawnPoint - respawnPoint).sqrMagnitude < 0.0001f)
                return false;

            var captured = new Snapshot { ScenePath = CurrentLevel.path, RespawnPoint = respawnPoint };
            foreach (var entry in inventory)
            {
                if (entry.Value == null) captured.Consumed.Add(entry.Key);
                else captured.States[entry.Key] = entry.Value.Capture();
            }

            SetSnapshot(captured);
            return true;
        }

        private void SetSnapshot(Snapshot next)
        {
            bool changed = snapshot != null || next != null;
            snapshot = next;
            if (changed) CheckpointChanged?.Invoke();
        }

        private bool CanReload() => State != GameState.Menu && State != GameState.Loading;

        private bool StartLoad(string scenePath, bool restore)
        {
            if (loading || string.IsNullOrEmpty(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath))
                return false;

            restoreOnLoad = restore && HasCheckpointFor(scenePath);
            continueRunOnLoad = restoreOnLoad;

            if (restore && State == GameState.GameOver)
                continueRunOnLoad = true;
            if (!restoreOnLoad) SetSnapshot(null);
            StartCoroutine(Load(scenePath));
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
                restoreOnLoad = false;
                continueRunOnLoad = false;
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
                TakeInventory(scene);

                bool restoring = restoreOnLoad && HasCheckpointFor(scene.path);
                restoreOnLoad = false;
                if (!restoring)
                    AttemptSeconds = 0f;

                if (!continueRunOnLoad)
                    levelDeaths.Remove(scene.name);

                continueRunOnLoad = false;
                if (!restoring) SetSnapshot(null);

                LevelStarted?.Invoke(scene);
                if (restoring) RestoreSnapshot();
            }

            SetState(scene.path == MenuPath ? GameState.Menu : GameState.Playing, true);
        }

        private void TakeInventory(Scene scene)
        {
            inventory.Clear();
            ids.Clear();
            var roots = new List<GameObject>(scene.GetRootGameObjects()) { transform.root.gameObject };
            foreach (var root in roots)
            {
                root.GetComponentsInChildren(true, found);
                foreach (var state in found)
                {
                    if (string.IsNullOrEmpty(state.Id) || !ids.Add(state.Id))
                    {
                        Debug.LogWarning(
                            $"GameManager: '{state.name}' has a missing or duplicate checkpoint ID and won't be saved. Run Lightfooted > Assign Checkpoint IDs In Build Levels.",
                            state);
                        continue;
                    }

                    inventory.Add(new KeyValuePair<string, CheckpointState>(state.Id, state));
                }
            }

            found.Clear();
        }

        private void RestoreSnapshot()
        {
            foreach (var entry in inventory)
            {
                if (snapshot.Consumed.Contains(entry.Key)) entry.Value.RestoreConsumed();
                else if (snapshot.States.TryGetValue(entry.Key, out var data)) entry.Value.Restore(data);
            }

            Respawned?.Invoke(snapshot.RespawnPoint);
        }

        private void EndLevel()
        {
            var ended = CurrentLevel;
            CurrentLevel = default;
            inLevel = false;
            inventory.Clear();
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
