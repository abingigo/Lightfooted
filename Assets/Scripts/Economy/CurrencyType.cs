using UnityEngine;

namespace Economy
{
    public enum CurrencyLifetime
    {
        Persistent,   // survives scene changes (until the game is quit, unless you add saving)
        PerLevel      // reset every time a level loads, including restarts
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