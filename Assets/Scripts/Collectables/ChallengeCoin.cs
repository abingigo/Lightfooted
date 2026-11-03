using Core;
using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(Collectable), typeof(CollectableState))]
    public sealed class ChallengeCoin : MonoBehaviour
    {
        [Tooltip("Tint multiplied onto the sprite when this coin was found on an earlier run.")]
        [SerializeField] private Color foundTint = new(0.45f, 0.45f, 0.45f, 0.6f);

        private ChallengeCoinRecord record;

        public string Id => GetComponent<CollectableState>().Id;

        private void Awake()
        {
            if (string.IsNullOrEmpty(Id))
                Debug.LogWarning($"{name}: challenge coin has no ID yet. Save the scene to assign one.", this);
            if (ServiceRegistry.TryGet(out IChallengeCoinService service)) record = service as ChallengeCoinRecord;
            if (record != null) record.Register(this);
        }

        private void Start()
        {
            if (record == null || !record.WasFound(this)) return;
            foreach (var sprite in GetComponentsInChildren<SpriteRenderer>(true)) sprite.color *= foundTint;
        }

        private void OnDestroy()
        {
            if (record != null) record.Unregister(this);
        }

        public bool Collect() => record != null && record.Collect(this);
    }
}
