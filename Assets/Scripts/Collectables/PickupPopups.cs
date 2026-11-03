using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(Collector))]
    public sealed class PickupPopups : MonoBehaviour
    {
        [Tooltip("Spawned where a pickup was collected, showing its definition's Pickup Message.")]
        [SerializeField] private PickupPopup popup;

        [Tooltip("Offset from the pickup's position where the popup starts.")]
        [SerializeField] private Vector3 offset = new(0f, 0.5f, 0f);

        private Collector collector;

        private void Awake() => collector = GetComponent<Collector>();
        private void OnEnable() => collector.Collected += Spawn;
        private void OnDisable() => collector.Collected -= Spawn;

        private void Spawn(CollectDefinition definition, Collectable source)
        {
            if (popup == null || string.IsNullOrEmpty(definition.PickupText)) return;
            var origin = source != null ? source.transform.position : transform.position;
            Instantiate(popup, origin + offset, Quaternion.identity).Show(definition.PickupText);
        }
    }
}
