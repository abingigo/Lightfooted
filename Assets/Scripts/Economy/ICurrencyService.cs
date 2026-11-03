using System;
using Core;

namespace Economy
{
    public interface ICurrencyService : IService
    {
        event Action<CurrencyType, int> BalanceChanged;
        int GetBalance(CurrencyType currency);
        int GetLevelEarnings(CurrencyType currency);
        void Add(CurrencyType currency, int amount);
        bool TrySpend(CurrencyType currency, int amount);
        void ResetBalances(CurrencyLifetime lifetime);
        string CaptureLevelEarnings();
        void RestoreLevelEarnings(string data);
    }
}
