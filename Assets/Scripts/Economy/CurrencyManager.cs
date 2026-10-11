using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Economy
{
    public class CurrencyManager : Service<ICurrencyService>, ICurrencyService
    {
        private readonly Dictionary<CurrencyType, int> balances = new();
        public event Action<CurrencyType, int> BalanceChanged;

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
            int balance = GetBalance(currency);
            if (balance < amount) return false;
            SetBalance(currency, balance - amount);
            return true;
        }

        void SetBalance(CurrencyType currency, int value)
        {
            balances[currency] = value;
            BalanceChanged?.Invoke(currency, value);
        }
    }
}