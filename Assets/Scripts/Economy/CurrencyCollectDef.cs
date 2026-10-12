using UnityEngine;
using Collectables;
using Core;

namespace Economy
{
    [CreateAssetMenu(menuName = "Lightfooted/Collectables/Currency Collectable", fileName = "NewCurrencyCollectable")]
    public class CurrencyCollectDef : CollectDefinition
    {
        [SerializeField] private CurrencyType currency;
        [SerializeField, Min(1)] private int amount = 1;

        public CurrencyType Currency => currency;
        public int Amount => amount;

        public override bool TryApply(Collector collector)
        {
            if (currency == null)
            {
                Debug.LogWarning($"{name}: no currency assigned.", this);
                return false;
            }
            var currencies = ServiceRegistry.Get<ICurrencyService>();
            if (currencies == null) return false;

            currencies.Add(currency, amount);
            return true;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (currency == null) Debug.LogWarning($"{name}: no currency assigned.", this);
        }
#endif
    }
}