using UnityEngine;

namespace Core
{
    public static class PersistentBootstrap
    {
        public const string ResourcePath = "PersistentManagers";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Spawn()
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"Core: no prefab at Resources/{ResourcePath}. Persistent services are missing.");
                return;
            }

            var instance = Object.Instantiate(prefab);
            instance.name = prefab.name;
            Object.DontDestroyOnLoad(instance);
        }
    }
}
