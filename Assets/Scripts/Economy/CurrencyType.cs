using UnityEngine;

namespace Economy
{
    [CreateAssetMenu(menuName = "Lightfooted/Economy/Currency  Type", fileName = "NewCurrencyType")]
    public class CurrencyType : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
    }
}