using System.Collections.Generic;
using UnityEngine;

public class PlayerThrowableLight : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] Transform holdPoint;
    [SerializeField] Vector3 holdOffset = new Vector3(0.6f, 0.3f, 0f);
    [SerializeField] float pickupRadius = 1.25f;
    [SerializeField] KeyCode pickupKey = KeyCode.E;
    [Tooltip("Hold to aim at the mouse, release to throw. Hold Down while pressing it to drop the light instead.")]
    [SerializeField] KeyCode throwKey = KeyCode.E;

    [Header("Throw")]
    [Tooltip("How far from the player, toward the aim, the light starts its flight.")]
    [SerializeField] float throwOffset = 1f;
    [Tooltip("Throw strength when aiming right next to the player.")]
    [SerializeField, Min(0f)] float minThrowImpulse = 3f;
    [Tooltip("Throw strength when aiming at Max Aim Distance or farther.")]
    [SerializeField, Min(0f)] float maxThrowImpulse = 13f;
    [Tooltip("Mouse distance from the player that gives full strength.")]
    [SerializeField, Min(0.1f)] float maxAimDistance = 7f;
    [SerializeField] float pickupDelay = 0.35f;

    ThrowableLight carriedLight;
    float facing = 1f;
    bool aiming;
    Vector2 aim = Vector2.right;
    readonly List<Vector2> throwPath = new List<Vector2>();

    Vector2 ThrowOrigin => (Vector2)transform.position + new Vector2(0f, holdOffset.y);

    void Update()
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked)
        {
            aiming = false;
            return;
        }
        float horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal != 0f) facing = Mathf.Sign(horizontal);
        if (carriedLight == null)
        {
            aiming = false;
            if (Input.GetKeyDown(pickupKey)) TryPickUp();
        }
        else
        {
            if (holdPoint == null)
                carriedLight.transform.position = transform.position + new Vector3(facing * holdOffset.x, holdOffset.y, holdOffset.z);

            UpdateThrowInput(MouseAim(), Input.GetAxisRaw("Vertical") < 0f,
                Input.GetKeyDown(throwKey), Input.GetKeyUp(throwKey));
        }
    }

    void OnDisable() => aiming = false;

    void OnApplicationFocus(bool focused)
    {
        if (!focused) aiming = false;
    }

    Vector2 MouseAim()
    {
        Camera cam = Camera.main;
        if (cam == null) return new Vector2(facing * maxAimDistance, 0f);
        return (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) - ThrowOrigin;
    }

    void UpdateThrowInput(Vector2 aimOffset, bool drop, bool pressed, bool released)
    {
        if (pressed)
        {
            if (drop)
            {
                carriedLight.Drop(pickupDelay);
                carriedLight = null;
                aiming = false;
                return;
            }

            aiming = true;
        }

        if (!aiming) return;
        aim = aimOffset;
        if (!released) return;

        GetThrow(out Vector2 position, out Vector2 impulse);
        carriedLight.Throw(position, impulse, pickupDelay);
        carriedLight = null;
        aiming = false;
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
        Vector2 direction = aim.sqrMagnitude > 0.0001f ? aim.normalized : new Vector2(facing, 0f);
        float strength = Mathf.Lerp(minThrowImpulse, maxThrowImpulse, Mathf.Clamp01(aim.magnitude / maxAimDistance));
        position = ThrowOrigin + direction * throwOffset;
        impulse = direction * strength;
    }

    void OnGUI()
    {
        if (carriedLight == null) return;
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;

        Camera cam = Camera.main;
        if (cam == null) return;
        if (aiming && Event.current.type == EventType.Repaint)
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
