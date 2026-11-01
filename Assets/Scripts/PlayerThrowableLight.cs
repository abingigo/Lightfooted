using UnityEngine;

public class PlayerThrowableLight : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] Transform holdPoint;
    [SerializeField] Vector3 holdOffset = new Vector3(0.6f, 0.3f, 0f);
    [SerializeField] float pickupRadius = 1.25f;
    [SerializeField] KeyCode pickupKey = KeyCode.E;
    [SerializeField] KeyCode throwKey = KeyCode.Q;

    [Header("Throw")]
    [SerializeField] float throwOffset = 1.3f;
    [SerializeField] float throwImpulse = 6f;
    [SerializeField] float upwardImpulse = 2f;
    [SerializeField] float pickupDelay = 0.35f;

    ThrowableLight carriedLight;
    float facing = 1f;

    void Update()
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;
        float horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal != 0f) facing = Mathf.Sign(horizontal);
        if (carriedLight == null)
        {
            if (Input.GetKeyDown(pickupKey)) TryPickUp();
        }
        else
        {
            if (holdPoint == null)
                carriedLight.transform.position = transform.position + new Vector3(facing * holdOffset.x, holdOffset.y, holdOffset.z);

            if (Input.GetKeyDown(throwKey)) Throw();
        }
    }

    void TryPickUp()
    {
        ThrowableLight nearest = null;
        float bestDistance = pickupRadius * pickupRadius;

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, pickupRadius))
        {
            ThrowableLight candidate = hit.GetComponentInParent<ThrowableLight>();
            if (candidate == null || !candidate.CanPickUp) continue;

            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                nearest = candidate;
                bestDistance = distance;
            }
        }

        if (nearest == null) return;

        nearest.PickUp(holdPoint != null ? holdPoint : transform,
            holdPoint != null ? Vector3.zero : new Vector3(facing * holdOffset.x, holdOffset.y, holdOffset.z));
        carriedLight = nearest;
    }

    void Throw()
    {
        ThrowableLight light = carriedLight;
        carriedLight = null;

        Vector2 position = (Vector2)transform.position + new Vector2(facing * throwOffset, holdOffset.y);
        Vector2 impulse = new Vector2(facing * throwImpulse, upwardImpulse);
        light.Throw(position, impulse, pickupDelay);
    }

    void OnGUI()
    {
        if (carriedLight == null || !carriedLight.IsOn) return;
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.2f);
        if (screen.z <= 0f) return;

        Color oldColor = GUI.contentColor;
        GUI.contentColor = Color.purple;

        GUI.Box(

        new Rect(screen.x - 35f, Screen.height - screen.y - 14f, 70f, 28f),
         carriedLight.TimeLeft.ToString("0.0")+ "s");
        GUI.contentColor = oldColor;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
