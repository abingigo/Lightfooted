using UnityEngine;

// Keeps physical contact with the level, while allowing the player to walk
// through a pickup or light source. Trigger colliders remain active.
public class IgnorePlayerCollision2D : MonoBehaviour
{
    Collider2D[] physicalColliders;
    Collider2D[] playerColliders;

    void Awake()
    {
        physicalColliders = GetComponents<Collider2D>();
    }

    void Start()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) playerColliders = player.GetComponents<Collider2D>();
        Apply();
    }

    void FixedUpdate()
    {
        // Unity can reset ignored pairs after a collider or body is disabled.
        Apply();
    }

    public void Apply()
    {
        if (playerColliders == null) return;
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider == null || !playerCollider.enabled || playerCollider.isTrigger) continue;
            foreach (Collider2D objectCollider in physicalColliders)
            {
                if (objectCollider == null || !objectCollider.enabled || objectCollider.isTrigger) continue;
                if (objectCollider.attachedRigidbody != null && !objectCollider.attachedRigidbody.simulated) continue;
                if (!Physics2D.GetIgnoreCollision(objectCollider, playerCollider))
                    Physics2D.IgnoreCollision(objectCollider, playerCollider, true);
            }
        }
    }
}
