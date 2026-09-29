using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    public Vector3 offset = new Vector3(0f, 0f, -10f);

    public float smoothSpeed = 8f;

    public float minX = -10f;
    public float maxX = 10f;

    public bool clampVertical = false;
    public float minY = -5f;
    public float maxY = 5f;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 desired = player.position + offset;
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        smoothed.x = ClampAxis(smoothed.x, minX, maxX, camHalfWidth);

        if (clampVertical)
        {
            smoothed.y = ClampAxis(smoothed.y, minY, maxY, camHalfHeight);
        }
        else
        {
            smoothed.y = transform.position.y;
        }

        smoothed.z = offset.z;

        transform.position = smoothed;
    }

    float ClampAxis(float desired, float levelMin, float levelMax, float camHalfExtent)
    {
        float clampMin = levelMin + camHalfExtent;
        float clampMax = levelMax - camHalfExtent;

        if (clampMin > clampMax)
            return (levelMin + levelMax) / 2f;

        return Mathf.Clamp(desired, clampMin, clampMax);
    }
}