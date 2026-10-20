using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BatteryItem : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float capacity = 10f;
    [SerializeField, Min(0f)] float startingEnergy = 10f;
    [SerializeField] SpriteRenderer chargeFill;

    Rigidbody2D body;
    float energy;
    bool available = true;
    Vector3 fullFillScale;
    Vector3 fullFillPosition;

    public float Energy => energy;
    public float Capacity => capacity;
    public bool IsAvailable => available;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        energy = Mathf.Clamp(startingEnergy, 0f, capacity);
        if (chargeFill == null) chargeFill = transform.Find("ChargeFill")?.GetComponent<SpriteRenderer>();
        if (chargeFill != null)
        {
            fullFillScale = chargeFill.transform.localScale;
            fullFillPosition = chargeFill.transform.localPosition;
            UpdateChargeVisual();
        }
    }

    public void Attach(Transform parent, Vector3 localPosition)
    {
        available = false;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;
        transform.SetParent(parent, false);
        transform.localPosition = localPosition;
    }

    public void Consume(float amount)
    {
        energy = Mathf.Max(0f, energy - Mathf.Max(0f, amount));
        UpdateChargeVisual();
    }

    public void Recharge(float amount)
    {
        energy = Mathf.Min(capacity, energy + Mathf.Max(0f, amount));
        UpdateChargeVisual();
    }

    void UpdateChargeVisual()
    {
        if (chargeFill == null) return;

        float fraction = Mathf.Clamp01(energy / capacity);
        chargeFill.enabled = fraction > 0f;

        Vector3 scale = fullFillScale;
        scale.x *= fraction;
        chargeFill.transform.localScale = scale;

        Vector3 position = fullFillPosition;
        position.x -= (fullFillScale.x - scale.x) * 0.5f;
        chargeFill.transform.localPosition = position;
    }

    void OnValidate()
    {
        capacity = Mathf.Max(0.1f, capacity);
        startingEnergy = Mathf.Clamp(startingEnergy, 0f, capacity);
    }
}
