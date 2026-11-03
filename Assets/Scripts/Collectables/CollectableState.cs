using Core;
using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(Collectable))]
    public sealed class CollectableState : CheckpointState
    {
        private const string Collected = "collected";

        public override string Capture() => GetComponent<Collectable>().IsCollected ? Collected : string.Empty;

        public override void Restore(string data)
        {
            if (data == Collected) RestoreConsumed();
        }

        public override void RestoreConsumed() => gameObject.SetActive(false);
    }
}
