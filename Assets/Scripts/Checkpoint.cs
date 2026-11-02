using Core;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var body = other.attachedRigidbody != null ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody2D>();
        if (body == null || !body.CompareTag("Player")) return;
        if (ServiceRegistry.TryGet(out IGameService game)) game.ReportCheckpoint(transform.position);
    }
}
