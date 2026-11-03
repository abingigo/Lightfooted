using Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("Light tinted while this is the active checkpoint. Found on this object if left empty.")]
    [SerializeField] Light2D glow;

    [Tooltip("Glow colour while this is the active checkpoint.")]
    [SerializeField] Color activeColor = new Color(0.45f, 1f, 0.6f, 1f);

    Collider2D trigger;
    Color idleColor;
    IGameService game;

    void Awake()
    {
        trigger = GetComponent<Collider2D>();
        if (glow == null) glow = GetComponentInChildren<Light2D>();
        if (glow != null) idleColor = glow.color;
    }

    void OnEnable()
    {
        if (ServiceRegistry.TryGet(out game)) game.CheckpointChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (game != null) game.CheckpointChanged -= Refresh;
        game = null;
    }

    bool IsActive =>
        game != null && game.HasCheckpointFor(gameObject.scene.path) &&
        (game.CheckpointPosition - transform.position).sqrMagnitude < 0.0001f;

    void Refresh()
    {
        bool active = IsActive;
        if (trigger != null) trigger.enabled = !active;
        if (glow != null) glow.color = active ? activeColor : idleColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var body = other.attachedRigidbody != null ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody2D>();
        if (body == null || !body.CompareTag("Player")) return;
        if (game != null) game.ReportCheckpoint(transform.position);
    }
}
