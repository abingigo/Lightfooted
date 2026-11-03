using Core;

[UnityEngine.RequireComponent(typeof(LanternPickup))]
public class LanternPickupState : CheckpointState
{
    public override string Capture() => string.Empty;

    public override void Restore(string data) { }

    public override void RestoreConsumed()
    {
        var pickup = GetComponent<LanternPickup>();
        if (pickup != null && pickup.platform != null) pickup.platform.isTrigger = false;
        base.RestoreConsumed();
    }
}
