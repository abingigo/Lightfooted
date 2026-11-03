using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;

namespace Economy
{
    public class CurrencyManager : Service<ICurrencyService>, ICurrencyService
    {
        [Serializable]
        private struct Entry
        {
            public string currency;
            public int amount;
        }

        [Serializable]
        private struct Earnings
        {
            public List<Entry> entries;
        }

        private readonly Dictionary<CurrencyType, int> wallet = new();
        private readonly Dictionary<CurrencyType, int> earned = new();
        private readonly Dictionary<string, CurrencyType> known = new();
        private readonly List<CurrencyType> changed = new();
        private IGameService game;

        public event Action<CurrencyType, int> BalanceChanged;

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

        protected override void OnLevelStarted(Scene level) => DiscardLevel();

        public int GetBalance(CurrencyType currency) =>
            currency == null ? 0 : Get(wallet, currency) + Get(earned, currency);

        public int GetLevelEarnings(CurrencyType currency) => currency == null ? 0 : Get(earned, currency);

        public void Add(CurrencyType currency, int amount)
        {
            if (currency == null || amount <= 0) return;
            Remember(currency);
            earned[currency] = Get(earned, currency) + amount;
            Raise(currency);
        }

        public bool TrySpend(CurrencyType currency, int amount)
        {
            if (currency == null || amount <= 0 || GetBalance(currency) < amount) return false;
            Remember(currency);
            earned[currency] = Get(earned, currency) - amount;
            Raise(currency);
            return true;
        }

        public void ResetBalances(CurrencyLifetime lifetime)
        {
            changed.Clear();
            foreach (var currency in known.Values)
                if (currency.Lifetime == lifetime && (wallet.Remove(currency) | earned.Remove(currency)))
                    changed.Add(currency);
            foreach (var currency in changed) Raise(currency);
        }

        public string CaptureLevelEarnings()
        {
            var data = new Earnings { entries = new List<Entry>() };
            foreach (var pair in earned) data.entries.Add(new Entry { currency = pair.Key.name, amount = pair.Value });
            return JsonUtility.ToJson(data);
        }

        public void RestoreLevelEarnings(string data)
        {
            DiscardLevel();
            var restored = JsonUtility.FromJson<Earnings>(data);
            if (restored.entries == null) return;
            foreach (var entry in restored.entries)
            {
                if (!known.TryGetValue(entry.currency, out var currency)) continue;
                earned[currency] = entry.amount;
                Raise(currency);
            }
        }

        private void CommitLevel()
        {
            changed.Clear();
            foreach (var pair in earned)
                if (pair.Key.Lifetime == CurrencyLifetime.Persistent)
                    changed.Add(pair.Key);
            foreach (var currency in changed)
            {
                wallet[currency] = Get(wallet, currency) + earned[currency];
                earned.Remove(currency);
            }
        }

        private void DiscardLevel()
        {
            changed.Clear();
            changed.AddRange(earned.Keys);
            earned.Clear();
            foreach (var currency in changed) Raise(currency);
        }

        private void Remember(CurrencyType currency)
        {
            if (known.TryGetValue(currency.name, out var existing) && existing != currency)
                Debug.LogWarning($"CurrencyManager: two currencies are named '{currency.name}'; checkpoints can't tell them apart.", currency);
            known[currency.name] = currency;
        }

        private void Raise(CurrencyType currency) => BalanceChanged?.Invoke(currency, GetBalance(currency));

        private static int Get(Dictionary<CurrencyType, int> source, CurrencyType currency) =>
            source.TryGetValue(currency, out int value) ? value : 0;
    }
}
