using UnityEngine;

namespace Collectables
{
    public abstract class CollectDefinition : ScriptableObject
    {
        [SerializeField] private Collectable prefabOverride;
        [SerializeField] private Sprite sprite;

        [Tooltip("Multiplied into every sprite on the collectable. White keeps the prefab's colours.")]
        [SerializeField] private Color tint = Color.white;

        [Tooltip("Shown in a popup where this is collected. Empty = no popup.")]
        [SerializeField] private string pickupMessage;

        public Collectable PrefabOverride => prefabOverride;
        public Sprite Sprite => sprite;
        public Color Tint => tint;
        public string PickupText => pickupMessage;
        
        public abstract bool TryApply(Collector collector);

        public virtual bool TryApply(Collector collector, Collectable source) => TryApply(collector);
    }
}
