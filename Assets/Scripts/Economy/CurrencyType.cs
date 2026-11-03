using UnityEngine;

namespace Economy
{
    public enum CurrencyLifetime
    {
        Persistent,   // added to the wallet when a level is completed; lost on restart or quit
        PerLevel      // only lasts for the current level; kept at checkpoints
    }
    
    [CreateAssetMenu(menuName = "Lightfooted/Economy/Currency Type", fileName = "NewCurrencyType")]
    public class CurrencyType : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] CurrencyLifetime lifetime = CurrencyLifetime.Persistent;
        
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public CurrencyLifetime Lifetime => lifetime;
    }
}