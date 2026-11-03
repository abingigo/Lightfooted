using System;
using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Collectables
{
    public sealed class ChallengeCoinRecord : Service<IChallengeCoinService>, IChallengeCoinService
    {
        [Serializable]
        private struct Attempt
        {
            public List<string> collected;
            public List<string> newFinds;
        }

        private readonly Dictionary<string, HashSet<string>> found = new();
        private readonly HashSet<string> collected = new();
        private readonly HashSet<string> newFinds = new();
        private readonly List<ChallengeCoin> coins = new();
        private IGameService game;

        public event Action ProgressChanged;

        public int CoinsInLevel
        {
            get
            {
                coins.RemoveAll(coin => coin == null);
                var level = CurrentLevel;
                int count = 0;
                foreach (var coin in coins)
                    if (coin.gameObject.scene == level) count++;
                return count;
            }
        }

        public int CollectedInLevel => collected.Count;

        private Scene CurrentLevel => game != null ? game.CurrentLevel : SceneManager.GetActiveScene();

        protected override void Start()
        {
            base.Start();
            if (ServiceRegistry.TryGet(out game)) game.GoalReached += CommitLevel;
        }

        protected override void OnDestroy()
        {
            if (game != null) game.GoalReached -= CommitLevel;
            base.OnDestroy();
        }

        protected override void OnLevelStarted(Scene level)
        {
            collected.Clear();
            newFinds.Clear();
            ProgressChanged?.Invoke();
        }

        public int FoundIn(string scenePath) =>
            scenePath != null && found.TryGetValue(scenePath, out var ids) ? ids.Count : 0;

        public bool WasFound(ChallengeCoin coin) =>
            found.TryGetValue(coin.gameObject.scene.path, out var ids) && ids.Contains(coin.Id);

        public void Register(ChallengeCoin coin)
        {
            if (coins.Contains(coin)) return;
            coins.Add(coin);
            ProgressChanged?.Invoke();
        }

        public void Unregister(ChallengeCoin coin)
        {
            if (coins.Remove(coin)) ProgressChanged?.Invoke();
        }

        public bool Collect(ChallengeCoin coin)
        {
            collected.Add(coin.Id);
            bool firstFind = !WasFound(coin) && newFinds.Add(coin.Id);
            ProgressChanged?.Invoke();
            return firstFind;
        }

        public string CaptureAttempt() =>
            JsonUtility.ToJson(new Attempt { collected = new List<string>(collected), newFinds = new List<string>(newFinds) });

        public void RestoreAttempt(string data)
        {
            var attempt = JsonUtility.FromJson<Attempt>(data);
            collected.Clear();
            newFinds.Clear();
            if (attempt.collected != null) collected.UnionWith(attempt.collected);
            if (attempt.newFinds != null) newFinds.UnionWith(attempt.newFinds);
            ProgressChanged?.Invoke();
        }

        private void CommitLevel()
        {
            if (newFinds.Count == 0) return;
            string path = CurrentLevel.path;
            if (!found.TryGetValue(path, out var ids)) found[path] = ids = new HashSet<string>();
            ids.UnionWith(newFinds);
            newFinds.Clear();
            ProgressChanged?.Invoke();
        }
    }
}
