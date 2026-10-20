using Core;
using UnityEngine;

namespace Lighting
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class LightGravity : MonoBehaviour
    {
        [Tooltip("The collider tested for gravity lights.")] [SerializeField]
        private Collider2D sensor;

        private Rigidbody2D body;
        private float baseGravity;

        private void Reset() => sensor = GetComponent<Collider2D>();

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            baseGravity = body.gravityScale;
        }

        private void OnDisable() => body.gravityScale = baseGravity;

        private void FixedUpdate()
        {
            if (!ServiceRegistry.TryGet(out ILightService lights)) return;
            body.gravityScale = lights.TryGetEffect(sensor, out GravityEffect gravity)
                ? baseGravity * gravity.Multiplier
                : baseGravity;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sensor == null) Debug.LogWarning($"{name}: LightGravity has no sensor collider.", this);
        }
#endif
    }
}