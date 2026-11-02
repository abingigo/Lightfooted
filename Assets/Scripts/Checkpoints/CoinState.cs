using Core;
using UnityEngine;

[RequireComponent(typeof(Coin))]
public class CoinState : CheckpointState
{
    const string Collected = "collected";

    bool IsCollected
    {
        get
        {
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                if (collider.enabled) return false;
            return true;
        }
    }

    public override string Capture() => IsCollected ? Collected : string.Empty;

    public override void Restore(string data)
    {
        if (data == Collected) RestoreConsumed();
    }

    public override void RestoreConsumed()
    {
        LevelStats.CollectCoin(gameObject.scene.name, GetComponent<Coin>().GetInstanceID());
        base.RestoreConsumed();
    }
}
