using Core;
using LightPlatformer.Obstacles;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class AnalyticsCheckpoint : MonoBehaviour
{
    [Tooltip("Stable ID, unique within this level. This records progress; it does not set a respawn point.")]
    public string checkpointId = "checkpoint-01";
    BoxCollider2D area;
    void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }
    void Awake() { area = GetComponent<BoxCollider2D>(); }
    void OnTriggerEnter2D(Collider2D other) { Record(other); }
    void OnTriggerStay2D(Collider2D other) { Record(other); }
    void Record(Collider2D other)
    {
        if (HazardContact.BodyTouches(other, area) && ServiceRegistry.TryGet<IAnalyticsService>(out var analytics))
            analytics.ReachCheckpoint(checkpointId);
    }
}
