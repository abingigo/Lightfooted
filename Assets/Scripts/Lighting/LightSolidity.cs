using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
    // Switches colliders on while a LightExposure is exposed. Polls after the LightManager's tick.
    [DefaultExecutionOrder(100)]
    public sealed class LightSolidity : MonoBehaviour
    {
        private const float RisingSpeed = 0.01f;

        [Tooltip("Whose light state decides solidity. Can be on another object (the feet use the body's).")]
        [SerializeField]
        private LightExposure exposure;

        [Tooltip("Colliders that are solid while exposed and off while dark.")] [SerializeField]
        private Collider2D[] targets = { };

        [Tooltip("Stay off while moving upward, so jumps pass up through platforms (for the feet).")] [SerializeField]
        private bool oneWay;

        [Tooltip("When lit inside a surface up to this deep, push the player out toward its center instead of phasing. 0 = off.")]
        [SerializeField, Min(0f)]
        private float maxSnapDepth;

        [Tooltip("Seconds a snap takes.")] [SerializeField, Min(0.01f)]
        private float snapDuration = 0.1f;

        private readonly List<Collider2D> blockers = new();
        private Rigidbody2D body;
        private float snapSpeed;

        private void Awake() => body = GetComponentInParent<Rigidbody2D>();

        // Read the state directly instead of waiting for an event: a disabled exposure doesn't announce its reset.
        private void OnEnable() => Apply();

        private void FixedUpdate() => Apply();

        private void Apply()
        {
            bool rising = oneWay && body && IsRising();
            bool wanted = exposure && exposure.IsExposed && !rising;
            bool snapping = false;

            foreach (var target in targets)
            {
                if (!target || target.enabled == wanted) continue;
                // Turning off is always safe; turning on waits until it wouldn't start inside something solid.
                if (wanted && IsBlocked(target, Vector2.zero))
                {
                    if (!snapping) snapping = TrySnap(target);
                    continue;
                }

                target.enabled = wanted;
            }

            if (!snapping) snapSpeed = 0f;
        }

        private bool TrySnap(Collider2D target)
        {
            if (maxSnapDepth <= 0f || !body) return false;

            Vector2 dir = SnapDirection(target);
            float depth = SunkDepth(target, dir);
            if (depth < 0f) return false;

            if (snapSpeed <= 0f) snapSpeed = Mathf.Max(depth, Physics2D.defaultContactOffset) / snapDuration;
            body.position += dir * Mathf.Min(depth, snapSpeed * Time.fixedDeltaTime);

            body.linearVelocity -= dir * Mathf.Min(0f, Vector2.Dot(body.linearVelocity, dir));
            body.totalForce -= dir * Mathf.Min(0f, Vector2.Dot(body.totalForce, dir));

            float gravityInto = Vector2.Dot(Physics2D.gravity * body.gravityScale, dir);
            if (gravityInto < 0f) body.linearVelocity -= dir * (gravityInto * Time.fixedDeltaTime);
            return true;
        }

        private Vector2 SnapDirection(Collider2D target)
        {
            Vector2 toCenter = body.position - (Vector2)target.transform.TransformPoint(target.offset);
            return toCenter.sqrMagnitude > 0.0001f ? toCenter.normalized : Up();
        }

        private float SunkDepth(Collider2D target, Vector2 dir)
        {
            if (IsBlocked(target, dir * maxSnapDepth)) return -1f;

            float low = 0f, high = maxSnapDepth;
            for (int i = 0; i < 8; i++)
            {
                float mid = (low + high) * 0.5f;
                if (IsBlocked(target, dir * mid)) low = mid;
                else high = mid;
            }

            return high;
        }

        private Vector2 Up() => Physics2D.gravity.y * body.gravityScale <= 0f ? Vector2.up : Vector2.down;

        private bool IsRising() => Vector2.Dot(body.linearVelocity, Up()) > RisingSpeed;

        private bool IsBlocked(Collider2D target, Vector2 shift)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(target.gameObject.layer));

            // Inset so a surface it's merely resting on doesn't count as overlapping.
            ShapeQuery.Overlap(target, filter, blockers, Physics2D.defaultContactOffset * 2f, shift);
            foreach (var hit in blockers)
                if (!body || hit.attachedRigidbody != body)
                    return true; // ignore our own colliders
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (exposure == null) Debug.LogWarning($"{name}: LightSolidity has no exposure.", this);
        }
#endif
    }
}