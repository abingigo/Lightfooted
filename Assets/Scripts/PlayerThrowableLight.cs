using System.Collections.Generic;
using UnityEngine;

public class PlayerThrowableLight : MonoBehaviour
{
    const float TapTime = 0.1f;

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
    [SerializeField, Min(0.01f)] float chargeTime = 0.75f;
    [SerializeField, Min(1f)] float maxThrowMultiplier = 2f;

    ThrowableLight carriedLight;
    float facing = 1f;
    float chargeDirection;
    float chargeStartedAt;
    readonly List<Vector2> throwPath = new List<Vector2>();

    void Update()
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked)
        {
            chargeDirection = 0f;
            return;
        }
        float horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal != 0f) facing = Mathf.Sign(horizontal);
        if (carriedLight == null)
        {
            chargeDirection = 0f;
            if (Input.GetKeyDown(pickupKey)) TryPickUp();
        }
        else
        {
            if (holdPoint == null)
                carriedLight.transform.position = transform.position + new Vector3(facing * holdOffset.x, holdOffset.y, holdOffset.z);

            UpdateThrowInput(horizontal, Input.GetKeyDown(throwKey), Input.GetKeyUp(throwKey));
        }
    }

    void OnDisable() => chargeDirection = 0f;

    void OnApplicationFocus(bool focused)
    {
        if (!focused) chargeDirection = 0f;
    }

    void UpdateThrowInput(float horizontal, bool pressed, bool released)
    {
        if (pressed)
        {
            if (horizontal == 0f)
            {
                carriedLight.Drop(pickupDelay);
                carriedLight = null;
                chargeDirection = 0f;
                return;
            }

            chargeDirection = Mathf.Sign(horizontal);
            chargeStartedAt = Time.time;
        }

        if (chargeDirection == 0f) return;
        if (horizontal != 0f) chargeDirection = Mathf.Sign(horizontal);
        if (!released) return;

        GetThrow(out Vector2 position, out Vector2 impulse);
        carriedLight.Throw(position, impulse, pickupDelay);
        carriedLight = null;
        chargeDirection = 0f;
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

    void GetThrow(out Vector2 position, out Vector2 impulse)
    {
        float charge = Mathf.Clamp01((Time.time - chargeStartedAt - TapTime) / chargeTime);
        float strength = Mathf.Lerp(1f, maxThrowMultiplier, charge);
        position = (Vector2)transform.position + new Vector2(chargeDirection * throwOffset, holdOffset.y);
        impulse = new Vector2(chargeDirection * throwImpulse * strength, upwardImpulse);
    }

    void OnGUI()
    {
        if (carriedLight == null) return;
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;

        Camera cam = Camera.main;
        if (cam == null) return;
        if (chargeDirection != 0f && Time.time - chargeStartedAt >= TapTime && Event.current.type == EventType.Repaint)
            DrawThrowPreview(cam);

        if (!carriedLight.IsOn) return;
        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.2f);
        if (screen.z <= 0f) return;

        Color oldColor = GUI.contentColor;
        GUI.contentColor = Color.purple;

        GUI.Box(

        new Rect(screen.x - 35f, Screen.height - screen.y - 14f, 70f, 28f),
         carriedLight.TimeLeft.ToString("0.0")+ "s");
        GUI.contentColor = oldColor;
    }

    void DrawThrowPreview(Camera camera)
    {
        GetThrow(out Vector2 position, out Vector2 impulse);
        bool hasLanding = carriedLight.PredictThrow(position, impulse, transform, throwPath, out Vector2 landing);
        foreach (Vector2 point in throwPath)
            DrawPreviewPoint(camera, point, false);
        if (hasLanding)
            DrawPreviewPoint(camera, landing, true);
    }

    static void DrawPreviewPoint(Camera camera, Vector2 point, bool landing)
    {
        Vector3 screen = camera.WorldToScreenPoint(point);
        if (screen.z <= 0f) return;

        float size = landing ? 20f : 6f;
        Rect rect = new Rect(screen.x - size * 0.5f, Screen.height - screen.y - size * 0.5f, size, size);
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f,
            Color.black, landing ? 5f : 0f, size * 0.5f);
        rect = new Rect(rect.x + 1f, rect.y + 1f, size - 2f, size - 2f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f,
            new Color(1f, 0.77f, 0.25f), landing ? 3f : 0f, rect.width * 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
