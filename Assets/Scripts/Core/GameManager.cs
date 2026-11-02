using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public sealed class GameManager : Service<IGameService>, IGameService
    {
        private bool inLevel;

        public Scene CurrentLevel { get; private set; }

        public event Action<Scene> LevelStarted;
        public event Action<Scene> LevelEnded;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFirstLevel()
        {
            if (ServiceRegistry.TryGet(out IGameService service) && service is GameManager manager)
                manager.StartLevel(SceneManager.GetActiveScene());
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
            if (inLevel && scene == CurrentLevel) return;
            if (inLevel) EndLevel();

            CurrentLevel = scene;
            inLevel = true;
            LevelStarted?.Invoke(scene);
        }

        private void EndLevel()
        {
            var ended = CurrentLevel;
            CurrentLevel = default;
            inLevel = false;
            LevelEnded?.Invoke(ended);
        }
    }
}
