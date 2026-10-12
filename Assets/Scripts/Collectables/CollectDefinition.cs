using UnityEngine;

namespace Collectables
{
    public abstract class CollectDefinition : ScriptableObject
    {
        [SerializeField] private Collectable prefabOverride;
        [SerializeField] private Sprite sprite;

        public Collectable PrefabOverride => prefabOverride;
        public Sprite Sprite => sprite;
        
        public abstract bool TryApply(Collector collector);
    }
}
