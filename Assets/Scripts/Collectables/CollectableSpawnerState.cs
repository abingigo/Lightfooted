using Core;
using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(CollectableSpawner))]
    public sealed class CollectableSpawnerState : CheckpointState
    {
        private const string Collected = "collected";

        public override string Capture() => GetComponent<CollectableSpawner>().HasBeenCollected ? Collected : string.Empty;

        public override void Restore(string data)
        {
            if (data == Collected) GetComponent<CollectableSpawner>().RestoreCollected();
        }

        public override void RestoreConsumed() { }
    }
}
