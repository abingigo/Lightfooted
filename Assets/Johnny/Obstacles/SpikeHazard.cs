using UnityEngine;

namespace LightPlatformer.Obstacles
{
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class SpikeHazard : MonoBehaviour
    {
        Collider2D hazard;
        void Awake() { hazard = GetComponent<PolygonCollider2D>(); }
        void OnTriggerEnter2D(Collider2D other) { HazardContact.Hurt(other, hazard); }
        void OnTriggerStay2D(Collider2D other) { HazardContact.Hurt(other, hazard); }
    }
}
