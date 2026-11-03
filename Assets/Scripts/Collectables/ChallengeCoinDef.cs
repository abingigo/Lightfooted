using Core;
using Economy;
using UnityEngine;

namespace Collectables
{
    [CreateAssetMenu(menuName = "Lightfooted/Collectables/Challenge Coin", fileName = "ChallengeCoin")]
    public sealed class ChallengeCoinDef : CollectDefinition
    {
        [Tooltip("Gains 1 the first time each coin is found. Kept once the level is completed.")]
        [SerializeField] private CurrencyType currency;

        public override bool TryApply(Collector collector)
        {
            Debug.LogWarning($"{name}: challenge coins must be placed in the scene with a ChallengeCoin component.", this);
            return false;
        }

        public override bool TryApply(Collector collector, Collectable source)
        {
            if (source == null || !source.TryGetComponent(out ChallengeCoin coin)) return TryApply(collector);
            if (coin.Collect() && currency != null && ServiceRegistry.TryGet(out ICurrencyService currencies))
                currencies.Add(currency, 1);
            return true;
        }
    }
}
