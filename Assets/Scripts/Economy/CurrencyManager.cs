using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement; 
using Core;

namespace Economy
{
    public class CurrencyManager : Service<ICurrencyService>, ICurrencyService
    {
        private readonly Dictionary<CurrencyType, int> balances = new();
        public event Action<CurrencyType, int> BalanceChanged;

        private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            ResetBalances(CurrencyLifetime.PerLevel);
        }
        public int GetBalance(CurrencyType currency) =>
            currency != null && balances.TryGetValue(currency, out var balance) ? balance : 0;

        public void Add(CurrencyType currency, int amount)
        {
            if (currency == null || amount <= 0) return;
            SetBalance(currency, GetBalance(currency) + amount);
        }

        public bool TrySpend(CurrencyType currency, int amount)
        {
            if (currency == null || amount <= 0) return false;
            var balance = GetBalance(currency);
            if (balance < amount) return false;
            SetBalance(currency, balance - amount);
            return true;
        }

        void SetBalance(CurrencyType currency, int value)
        {
            balances[currency] = value;
            BalanceChanged?.Invoke(currency, value);
        }
        public void ResetBalances(CurrencyLifetime lifetime)
        {
            var toReset = balances.Keys.Where(currency => currency.Lifetime == lifetime).ToList();

            foreach (var currency in toReset)
                SetBalance(currency, 0);
        }
    }
}