using UnityEngine;

/// Attach directly to the LIGHT object itself, which also has the
/// BoxCollider2D component on it (not a separate child). This script only
/// ever edits the collider's own size/offset -- never the transform's scale
/// -- so any sprite/visual on this same object is left completely alone.
[RequireComponent(typeof(BoxCollider2D))]
public class LightColliderWindow : MonoBehaviour
{
    public Collider2D platform;

    public float radius = 1.5f;

    BoxCollider2D col;

    public BlinkingLight blinkingLight;

    // Original values from the Editor -- width is never modified.
    float baseWidth;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        baseWidth = col.size.x;
    }

    void LateUpdate()
    {
        // Unity's own null check -- catches a destroyed-but-still-referenced
        // object, which plain C# "== null" / "is null" would miss.
        if (platform == null)
        {
            col.enabled = false;
            return;
        }

        float platformMinX = platform.bounds.min.x;
        float platformMaxX = platform.bounds.max.x;

        float lightX = transform.position.x;

        bool onPlatform = (lightX + radius) > platformMinX && (lightX - radius) < platformMaxX;
        bool lightActive = blinkingLight == null || blinkingLight.IsOn;
        col.enabled = onPlatform && lightActive;
        if (!col.enabled) return;

        float minCenter = platformMinX + baseWidth / 2f;
        float maxCenter = platformMaxX - baseWidth / 2f;

        float centerX;
        if (minCenter > maxCenter)
        {
            centerX = (platformMinX + platformMaxX) / 2f;
        }
        else
        {
            centerX = Mathf.Clamp(lightX, minCenter, maxCenter);
        }

        float platformTop = platform.bounds.max.y;
        float platformBottom = platform.bounds.min.y;
        float platformHeightWorld = platformTop - platformBottom;
        float platformCenterY = (platformTop + platformBottom) / 2f;

        float scaleY = transform.lossyScale.y;
        float sizeY = Mathf.Approximately(scaleY, 0f) ? platformHeightWorld : platformHeightWorld / scaleY;

        Vector3 worldCenter = new Vector3(centerX, platformCenterY, transform.position.z);
        Vector3 localCenter = transform.InverseTransformPoint(worldCenter);

        col.size = new Vector2(baseWidth, sizeY);
        col.offset = new Vector2(localCenter.x, localCenter.y);
    }
}