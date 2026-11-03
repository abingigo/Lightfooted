using Core;
using UnityEngine;

namespace Economy
{
    public sealed class CurrencyDebugReadout : MonoBehaviour
    {
        [Tooltip("Currencies to show: balance (wallet + this level) and this level's earnings.")]
        [SerializeField] private CurrencyType[] currencies = { };

        [SerializeField] private Vector2 screenPosition = new(16f, 100f);

        private void OnGUI()
        {
            if (!ServiceRegistry.TryGet(out ICurrencyService service)) return;
            float y = screenPosition.y;
            foreach (var currency in currencies)
            {
                if (currency == null) continue;
                GUI.Label(new Rect(screenPosition.x, y, 400f, 22f),
                    $"{currency.DisplayName}: {service.GetBalance(currency)} (this level {service.GetLevelEarnings(currency)})");
                y += 22f;
            }
        }
    }
}
