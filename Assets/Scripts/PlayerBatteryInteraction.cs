using UnityEngine;

public class PlayerBatteryInteraction : MonoBehaviour
{
    [SerializeField] KeyCode interactKey = KeyCode.R;
    [SerializeField, Min(0.1f)] float interactRadius = 1.5f;
    [SerializeField] Transform batteryHoldPoint;
    [SerializeField] Vector3 holdOffset = new Vector3(-0.5f, 0.25f, 0f);

    BatteryItem heldBattery;

    void Update()
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;
        if (!Input.GetKeyDown(interactKey)) return;

        if (heldBattery != null)
        {
            BatteryPoweredLight emptyLight = FindNearestLight(false);
            if (emptyLight != null && emptyLight.Insert(heldBattery)) heldBattery = null;
            return;
        }

        BatteryPoweredLight loadedLight = FindNearestLight(true);
        if (loadedLight != null)
        {
            heldBattery = loadedLight.Remove();
            heldBattery.Attach(batteryHoldPoint != null ? batteryHoldPoint : transform,
                batteryHoldPoint != null ? Vector3.zero : holdOffset);
            return;
        }

        BatteryItem loose = FindNearestBattery();
        if (loose == null) return;
        heldBattery = loose;
        loose.Attach(batteryHoldPoint != null ? batteryHoldPoint : transform,
            batteryHoldPoint != null ? Vector3.zero : holdOffset);
    }

    BatteryPoweredLight FindNearestLight(bool loaded)
    {
        BatteryPoweredLight nearest = null;
        float best = interactRadius * interactRadius;
        foreach (BatteryPoweredLight light in FindObjectsByType<BatteryPoweredLight>(FindObjectsSortMode.None))
        {
            if (light.HasBattery != loaded) continue;
            float distance = ((Vector2)(light.transform.position - transform.position)).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            nearest = light;
        }
        return nearest;
    }

    BatteryItem FindNearestBattery()
    {
        BatteryItem nearest = null;
        float best = interactRadius * interactRadius;
        foreach (BatteryItem item in FindObjectsByType<BatteryItem>(FindObjectsSortMode.None))
        {
            if (!item.IsAvailable) continue;
            float distance = ((Vector2)(item.transform.position - transform.position)).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            nearest = item;
        }
        return nearest;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
