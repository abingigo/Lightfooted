using UnityEngine;

namespace System
{
    public abstract class Service<TService> : MonoBehaviour where TService : class, IService
    {
        [SerializeField] private bool persistAcrossScenes = false;

        protected virtual void Awake()
        {
            if (this is not TService self)
            {
                Debug.LogError($"{GetType().Name} must implement {typeof(TService).Name}");
                return;
            }
            if (!ServiceRegistry.Register(self)) { Destroy(gameObject); return; }
            if (persistAcrossScenes) { transform.SetParent(null); DontDestroyOnLoad(gameObject); }
        }
        protected virtual void OnDestroy()
        {
            if (this is TService self) ServiceRegistry.Unregister(self);
        }
    }
}
