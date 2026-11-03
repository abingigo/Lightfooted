using System;
using UnityEngine;

namespace Collectables
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Collector : MonoBehaviour
    {
        public event Action<CollectDefinition, Collectable> Collected;
        internal void NotifyCollected(CollectDefinition definition, Collectable source) => Collected?.Invoke(definition, source);
    }
}
