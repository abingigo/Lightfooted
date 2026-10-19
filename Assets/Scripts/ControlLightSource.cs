using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ControllableLight : MonoBehaviour
{
    public float turnSpeed = 720f;

    public float[] anglesClockwise = { 0f, -90f, 180f, 90f };

    public Collider2D clickCollider;

    public Light2D light2D;

    public float selectedIntensityMultiplier = 1.5f;

    int currentIndex = 0;
    float targetAngle;
    bool isSelected = false;
    public bool IsSelected => isSelected;
    float normalIntensity = 1f;
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        if (clickCollider == null) clickCollider = GetComponent<Collider2D>();
        if (light2D == null) light2D = GetComponent<Light2D>();
        if (light2D != null) normalIntensity = light2D.intensity;
    }

    void Start()
    {
        targetAngle = anglesClockwise[currentIndex];
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    void Update()
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;
        HandleClickSelection();

        if (isSelected && Input.GetKeyDown(KeyCode.F))
        {
            SetDirection((currentIndex + 1) % anglesClockwise.Length);
        }

        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    void HandleClickSelection()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 worldPoint = cam.ScreenToWorldPoint(Input.mousePosition);

        bool clickedMe = clickCollider != null && clickCollider.OverlapPoint(worldPoint);
        SetSelected(clickedMe);
    }

    void SetSelected(bool value)
    {
        isSelected = value;
        if (light2D != null)
            light2D.intensity = isSelected ? normalIntensity * selectedIntensityMultiplier : normalIntensity;
    }

    void SetDirection(int index)
    {
        currentIndex = index;
        targetAngle = anglesClockwise[currentIndex];
    }
}
