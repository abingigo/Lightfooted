using UnityEngine;

namespace Collectables
{
    public abstract class CollectDefinition : ScriptableObject
    {
        [SerializeField] private Collectable prefabOverride;
        [SerializeField] private Sprite sprite;

        [Tooltip("Shown in a popup where this is collected. Empty = no popup.")]
        [SerializeField] private string pickupMessage;

        public Collectable PrefabOverride => prefabOverride;
        public Sprite Sprite => sprite;
        public string PickupText => pickupMessage;
        
        public abstract bool TryApply(Collector collector);

        public virtual bool TryApply(Collector collector, Collectable source) => TryApply(collector);
    }
}
