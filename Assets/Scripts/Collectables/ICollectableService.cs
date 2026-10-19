using Core;
using UnityEngine;

namespace Collectables
{
    public interface ICollectableService : IService
    {
        Collectable Spawn(CollectDefinition definition, Vector3 position, Transform parent = null);
        void Release(Collectable collectable);
    }
}