using TMPro;
using UnityEngine;

namespace LightPlatformer.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class CoinCounter : MonoBehaviour
    {
        TMP_Text label;

        void Awake() => label = GetComponent<TMP_Text>();

        void OnEnable()
        {
            LevelStats.CoinProgressChanged += Refresh;
            Refresh();
        }

        void OnDisable() => LevelStats.CoinProgressChanged -= Refresh;

        void Refresh()
        {
            label.enabled = LevelStats.CoinTotal > 0;
            label.text = label.enabled ? "Coins " + LevelStats.CoinsCollected + "/" + LevelStats.CoinTotal : "";
        }
    }
}
