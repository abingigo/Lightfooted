using System;
using Core;

namespace Economy
{
    public interface ICurrencyService : IService
    {
        event Action<CurrencyType, int> BalanceChanged;
        int GetBalance(CurrencyType currency);
        void Add(CurrencyType currency, int amount);
        bool TrySpend(CurrencyType currency, int amount);
    }
}
