using Core;
using UnityEngine;

namespace Collectables
{ 
    [DisallowMultipleComponent]
    public sealed class CollectableSpawner : MonoBehaviour
    {
        [SerializeField] private CollectDefinition definition;
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool respawn;
        [SerializeField, Min(0f)] private float respawnDelay = 5f;

        private Collectable current;
        private float respawnAt = -1f; // < 0 = no respawn pending

        public Collectable Current => current;

        private void Start()
        {
            if (spawnOnStart) Spawn();
        }

        private void Update()
        {
        if (respawnAt < 0f || Time.time < respawnAt) return;
            respawnAt = -1f;
            Spawn();
        }

        public void Spawn()
        {
            if (current) return;
            var service = ServiceRegistry.Get<ICollectableService>();
            if (service == null) return;

            current = service.Spawn(definition, transform.position);
            if (current) current.Collected += OnCollected;
        }

        private void OnCollected(Collectable collectable)
        {
            collectable.Collected -= OnCollected;
            current = null;
            if (ServiceRegistry.TryGet(out ICollectableService service)) service.Release(collectable);
            if (respawn) respawnAt = Time.time + respawnDelay;
        }

        private void OnDestroy()
        {
            if (current == null) return;
            current.Collected -= OnCollected;
            if (ServiceRegistry.TryGet(out ICollectableService service)) service.Release(current);
            current = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = definition != null ? Color.yellow : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
