using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class BatteryPoweredLight : MonoBehaviour
{
    [SerializeField] Light2D light2D;
    [SerializeField] Collider2D lightRange;
    [SerializeField] Transform batterySocket;
    [SerializeField, Min(0f)] float energyPerSecond = 1f;

    BatteryItem battery;
    bool wasPowered;

    public bool HasBattery => battery != null;
    public bool IsPowered => battery != null && battery.Energy > 0f;

    void Awake()
    {
        if (light2D == null) light2D = GetComponent<Light2D>();
        gameObject.tag = "Untagged";

        if (lightRange == null)
        {
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
            {
                if (collider.transform == transform) continue;
                lightRange = collider;
                break;
            }
        }

        if (lightRange != null)
        {
            lightRange.isTrigger = true;
            lightRange.gameObject.tag = "Light";
        }
        else Debug.LogWarning("BatteryPoweredLight needs a child LightRange collider.", this);

        UpdatePower();
    }

    void Update()
    {
        if (IsPowered) battery.Consume(energyPerSecond * Time.deltaTime);
        UpdatePower();
    }

    public bool Insert(BatteryItem item)
    {
        if (battery != null || item == null) return false;
        battery = item;
        item.Attach(batterySocket != null ? batterySocket : transform, Vector3.zero);
        UpdatePower();
        return true;
    }

    public BatteryItem Remove()
    {
        BatteryItem item = battery;
        battery = null;
        UpdatePower();
        return item;
    }

    void UpdatePower()
    {
        bool powered = IsPowered;
        if (light2D != null) light2D.enabled = powered;
        if (lightRange != null) lightRange.enabled = powered;

        // A disabled trigger may not deliver OnTriggerExit2D to the player.
        if (wasPowered && !powered && lightRange != null)
        {
            foreach (PlayerLightTracker tracker in FindObjectsByType<PlayerLightTracker>(FindObjectsSortMode.None))
                tracker.ForgetLight(lightRange);
        }
        wasPowered = powered;
    }
}
