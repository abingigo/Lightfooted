using System.Collections.Generic;
using LightPlatformer.UI;
using TMPro;
using UnityEngine;

namespace LightPlatformer.Obstacles
{
    [RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
    public sealed class LightStoppingPlatform : MonoBehaviour
    {
        [Tooltip("World-space displacement from the starting position to the other endpoint.")]
        public Vector2 travelOffset = new Vector2(10, 0);
        [Min(0)] public float speed = 2;
        public SpriteRenderer surface;
        public TMP_Text stateLabel;
        public bool IsIlluminated { get; private set; }
        public Vector2 StartPosition { get; private set; }

        Rigidbody2D body;
        BoxCollider2D platform;
        bool towardEnd = true;
        readonly List<Collider2D> lightHits = new List<Collider2D>(16);
        readonly List<Collider2D> riderHits = new List<Collider2D>(16);
        readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();
        static readonly Color MovingColor = new Color32(165, 234, 203, 255);
        static readonly Color HeldColor = new Color32(255, 207, 115, 255);

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            platform = GetComponent<BoxCollider2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;
            body.freezeRotation = true;
            StartPosition = body.position;
            RefreshVisual();
        }

        void FixedUpdate()
        {
            if (GameMenuController.GameplayInputBlocked) return;
            // Recheck overlaps so disabled or destroyed lights release the platform.
            Physics2D.SyncTransforms();
            lightHits.Clear();
            platform.Overlap(new ContactFilter2D().NoFilter(), lightHits);
            bool lit = false;
            foreach (var hit in lightHits)
                if (hit != null && hit.enabled && hit.gameObject.activeInHierarchy && hit.CompareTag("Light"))
                { lit = true; break; }
            if (lit != IsIlluminated) { IsIlluminated = lit; RefreshVisual(); }
            if (lit || speed <= 0 || !platform.enabled) { body.linearVelocity = Vector2.zero; return; }

            Vector2 target = towardEnd ? StartPosition + travelOffset : StartPosition;
            Vector2 next = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
            CarryRiders(next - body.position);
            body.MovePosition(next);
            if ((next - target).sqrMagnitude < 0.000001f) towardEnd = !towardEnd;
        }

        void CarryRiders(Vector2 delta)
        {
            if (delta.sqrMagnitude == 0) return;
            var bounds = platform.bounds;
            riderHits.Clear(); riders.Clear();
            Physics2D.OverlapBox(new Vector2(bounds.center.x, bounds.max.y + 0.12f),
                new Vector2(Mathf.Max(0.01f, bounds.size.x - 0.06f), 0.24f), 0,
                new ContactFilter2D().NoFilter(), riderHits);
            foreach (var hit in riderHits)
            {
                if (!HazardContact.TryPlayerBox(hit, out var box) || !box.enabled) continue;
                var rider = box.attachedRigidbody;
                var feet = box.bounds;
                if (rider == null || rider.linearVelocity.y > 0.1f ||
                    Mathf.Abs(feet.min.y - bounds.max.y) > 0.12f ||
                    feet.max.x <= bounds.min.x || feet.min.x >= bounds.max.x || !riders.Add(rider)) continue;
                // Apply the platform's displacement to standing riders.
                rider.position += delta;
            }
        }

        void RefreshVisual()
        {
            var color = IsIlluminated ? HeldColor : MovingColor;
            if (surface != null) surface.color = color;
            if (stateLabel != null) { stateLabel.text = IsIlluminated ? "HELD BY LIGHT" : "MOVING"; stateLabel.color = color; }
        }

        void OnDrawGizmosSelected()
        {
            Vector2 start = Application.isPlaying ? StartPosition : (Vector2)transform.position;
            Gizmos.color = HeldColor;
            Gizmos.DrawLine(start, start + travelOffset);
            Gizmos.DrawWireSphere(start, 0.15f);
            Gizmos.DrawWireSphere(start + travelOffset, 0.15f);
        }
    }
}
