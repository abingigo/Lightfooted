using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ThrowableLight : MonoBehaviour
{
    [SerializeField] Collider2D lightRange;

    Rigidbody2D body;
    float nextPickupTime;

    public bool CanPickUp => body != null && body.simulated && Time.time >= nextPickupTime;
    public bool IsCarried => body != null && !body.simulated;
    public bool HasBeenThrown { get; private set; }
    public Collider2D LightRange => lightRange;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        // The root is a physical object; only its child trigger counts as light.
        gameObject.tag = "Untagged";
        if (lightRange == null)
        {
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            {
                if (collider.transform != transform)
                {
                    lightRange = collider;
                    break;
                }
            }
        }

        if (lightRange != null)
        {
            lightRange.isTrigger = true;
            lightRange.gameObject.tag = "Light";
        }
        else
        {
            Debug.LogWarning("ThrowableLight needs a child LightRange collider.", this);
        }
    }

    public void PickUp(Transform holdPoint, Vector3 fallbackOffset)
    {
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;
        transform.SetParent(holdPoint, true);
        transform.position = holdPoint.position + fallbackOffset;
    }

    public void Throw(Vector2 position, Vector2 impulse, float pickupDelay)
    {
        HasBeenThrown = true;
        transform.SetParent(null, true);
        transform.position = position;
        body.simulated = true;
        GetComponent<IgnorePlayerCollision2D>()?.Apply();
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        nextPickupTime = Time.time + pickupDelay;
        body.AddForce(impulse, ForceMode2D.Impulse);
    }
}
