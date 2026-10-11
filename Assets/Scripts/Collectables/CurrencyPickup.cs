using Core;
using Economy;
using UnityEngine;

namespace Collectables
{
    // Permanent Currency;
    public class CurrencyPickup : Collectable
    {
        [SerializeField] private CurrencyType currency;
        [SerializeField, Min(1)] private int amount = 1;

        protected override void OnCollected(GameObject collector) =>
            ServiceRegistry.Get<ICurrencyService>()?.Add(currency, amount);
    }
}