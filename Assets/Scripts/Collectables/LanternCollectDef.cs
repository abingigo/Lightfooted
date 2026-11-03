using UnityEngine;

namespace Collectables
{
    [CreateAssetMenu(menuName = "Lightfooted/Collectables/Lantern", fileName = "Lantern")]
    public sealed class LanternCollectDef : CollectDefinition
    {
        [Tooltip("Seconds the lantern stays lit. Picking one up while lit refills it.")]
        [SerializeField, Min(0.1f)] private float duration = 5f;

        public override bool TryApply(Collector collector)
        {
            var lantern = collector.GetComponentInChildren<LanternTimer>(true);
            if (lantern == null) return false;

            lantern.Light(duration);
            var tracker = collector.GetComponent<PlayerLightTracker>();
            if (tracker != null)
            {
                tracker.hasLantern = true;
                tracker.UpdateCollision();
            }
            return true;
        }
    }
}
