using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public abstract class Service<TService> : MonoBehaviour where TService : class, IService
    {
        private IGameService game;

        protected virtual void Awake()
        {
            if (this is not TService self)
            {
                Debug.LogError($"{GetType().Name} must implement {typeof(TService).Name}");
                return;
            }

            if (!ServiceRegistry.Register(self)) Destroy(this);
        }

        protected virtual void Start()
        {
            if (this is IGameService || !ServiceRegistry.TryGet(out game)) return;

            game.LevelStarted += OnLevelStarted;
            game.LevelEnded += OnLevelEnded;
            if (game.CurrentLevel.IsValid()) OnLevelStarted(game.CurrentLevel);
        }

        protected virtual void OnLevelStarted(Scene level) { }

        protected virtual void OnLevelEnded(Scene level) { }

        protected virtual void OnDestroy()
        {
            if (game != null)
            {
                game.LevelStarted -= OnLevelStarted;
                game.LevelEnded -= OnLevelEnded;
            }

            if (this is TService self) ServiceRegistry.Unregister(self);
        }
    }
}
