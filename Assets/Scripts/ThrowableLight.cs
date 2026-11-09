using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D))]
public class ThrowableLight : MonoBehaviour
{
    [SerializeField] Collider2D lightRange;
    [SerializeField] float duration = 10f;

    float remainingTimeleft;
    Rigidbody2D body;
    Collider2D physicalCollider;
    float nextPickupTime;
    float turnOffTime;
    bool carried;
    bool timerStarted;
    readonly List<RaycastHit2D> previewHits = new List<RaycastHit2D>();

    public bool CanPickUp => body != null && body.simulated && !carried && Time.time >= nextPickupTime;
    public bool IsCarried => carried;
    public bool IsOn { get; private set; } = true;
    public bool HasBeenThrown { get; private set; }
    public Collider2D LightRange => lightRange;

    Light2D light;

    public float TimeLeft
    {
        get
        {
            if (!IsOn) return 0f;
            if (carried) return Mathf.Max(0f, turnOffTime - Time.time);
            return remainingTimeleft;
        }
    }

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        physicalCollider = GetComponent<Collider2D>();
        light = GetComponent<Light2D>();

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
        carried = true;

        // Keep the light range queryable while its physical body follows the player.
        body.bodyType = RigidbodyType2D.Kinematic;
        physicalCollider.enabled = false;
        transform.SetParent(holdPoint, true);
        transform.position = holdPoint.position + fallbackOffset;
        light.enabled = IsOn;
        lightRange.enabled = IsOn;

        if (!timerStarted)
        {
            timerStarted = true;
            remainingTimeleft = duration;
        }

        if (IsOn)
        {
            turnOffTime = Time.time + remainingTimeleft;
            Invoke(nameof(TurnOff), remainingTimeleft);
        }
    }

    public void Throw(Vector2 position, Vector2 impulse, float pickupDelay)
    {
        HasBeenThrown = true;
        Release(position, impulse, pickupDelay);
    }

    public void Drop(float pickupDelay)
    {
        Release(transform.position, Vector2.zero, pickupDelay);
    }

    internal bool PredictThrow(Vector2 position, Vector2 impulse, Transform player, List<Vector2> path,
        out Vector2 landing)
    {
        path.Clear();
        landing = default;
        if (physicalCollider is not CircleCollider2D circle) return false;

        Vector3 scale = circle.transform.lossyScale;
        float radius = circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        Vector2 center = position + (Vector2)circle.transform.TransformVector(circle.offset);
        Vector2 velocity = impulse / body.mass;
        float step = Time.fixedDeltaTime;
        Vector2 gravity = Physics2D.gravity * body.gravityScale;
        float damping = 1f / (1f + body.linearDamping * step);
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));

        int steps = Mathf.CeilToInt(2f / step);
        for (int i = 0; i < steps; i++)
        {
            velocity = Vector2.ClampMagnitude((velocity + gravity * step) * damping, Physics2D.maxTranslationSpeed);
            Vector2 travel = velocity * step;
            Physics2D.CircleCast(center, radius, travel.normalized, filter, previewHits, travel.magnitude);
            foreach (RaycastHit2D hit in previewHits)
            {
                if (hit.collider.transform.IsChildOf(player) || hit.collider.attachedRigidbody == body) continue;
                landing = hit.point;
                return true;
            }

            center += travel;
            if (i % 4 == 0) path.Add(center);
        }

        return false;
    }

    void Release(Vector3 position, Vector2 impulse, float pickupDelay)
    {
        remainingTimeleft = TimeLeft;
        CancelInvoke(nameof(TurnOff));

        if (timerStarted && remainingTimeleft <= 0f)
        {
            TurnOff();
        }

        carried = false;
        transform.SetParent(null, true);
        transform.position = position;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = true;
        physicalCollider.enabled = true;
        GetComponent<IgnorePlayerCollision2D>()?.Apply();
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        nextPickupTime = Time.time + pickupDelay;
        body.AddForce(impulse, ForceMode2D.Impulse);
        light.enabled = IsOn;
        lightRange.enabled = IsOn;
    }

    void TurnOff()
    {
        IsOn = false;
        light.enabled = false;
        lightRange.enabled = false;
    }
}
