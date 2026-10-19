using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Collectables
{
    public class CollectableManager : Service<ICollectableService>, ICollectableService
    {
        [SerializeField] private Collectable defaultPrefab;
        [SerializeField, Min(0)] private int defaultCapacity = 10;
        [SerializeField, Min(1)] private int maxPoolSize = 100;

        private readonly Dictionary<Collectable, ObjectPool<Collectable>> pools = new(); // prefab → its pool

        private readonly Dictionary<Collectable, ObjectPool<Collectable>>
            owners = new(); // live instance → pool it came from

        public Collectable Spawn(CollectDefinition definition, Vector3 position, Transform parent = null)
        {
            if (definition == null)
            {
                Debug.LogError("CollectableManager: Spawn called without a definition.", this);
                return null;
            }

            var prefab = definition.PrefabOverride != null ? definition.PrefabOverride : defaultPrefab;
            if (prefab == null)
            {
                Debug.LogError(
                    $"CollectableManager: {definition.name} has no prefab override and no default prefab is set.",
                    this);
                return null;
            }

            var pool = GetPool(prefab);
            var instance = pool.Get();
            owners[instance] = pool;

            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            instance.Initialize(definition);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(Collectable instance)
        {
            if (instance == null) return;
            if (!owners.Remove(instance, out var pool))
            {
                Debug.LogWarning($"CollectableManager: {instance.name} wasn't spawned by this manager.", instance);
                return;
            }

            pool.Release(instance);
        }

        private ObjectPool<Collectable> GetPool(Collectable prefab)
        {
            if (pools.TryGetValue(prefab, out var pool)) return pool;

            pool = new ObjectPool<Collectable>(
                createFunc: () =>
                {
                    var created = Instantiate(prefab, transform);
                    created.gameObject.SetActive(false);
                    return created;
                },
                actionOnRelease: released =>
                {
                    released.gameObject.SetActive(false);
                    released.transform.SetParent(transform, false);
                },
                actionOnDestroy: destroyed =>
                {
                    if (destroyed != null) Destroy(destroyed.gameObject);
                },
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize);

            pools.Add(prefab, pool);
            return pool;
        }

        protected override void OnDestroy()
        {
            foreach (var pool in pools.Values) pool.Clear();
            pools.Clear();
            owners.Clear();
            base.OnDestroy();
        }
    }
}