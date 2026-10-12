using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BatteryItem : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float capacity = 10f;
    [SerializeField, Min(0f)] float startingEnergy = 10f;

    Rigidbody2D body;
    float energy;
    bool available = true;

    public float Energy => energy;
    public float Capacity => capacity;
    public bool IsAvailable => available;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        energy = Mathf.Clamp(startingEnergy, 0f, capacity);
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
    }

    public void Recharge(float amount)
    {
        energy = Mathf.Min(capacity, energy + Mathf.Max(0f, amount));
    }

    void OnValidate()
    {
        capacity = Mathf.Max(0.1f, capacity);
        startingEnergy = Mathf.Clamp(startingEnergy, 0f, capacity);
    }
}
