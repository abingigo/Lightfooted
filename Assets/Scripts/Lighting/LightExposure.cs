using System;
using Core;
using UnityEngine;

namespace Lighting
{
    public class LightExposure : MonoBehaviour
    {
        [Tooltip("The collider whose shape is tested against light. Can be disabled; it's rebuilt from its geometry.")]
        [SerializeField]
        private Collider2D sensor;

        [Tooltip("Stay exposed for the LightManager's grace steps after leaving light (coyote time).")] [SerializeField]
        private bool useGrace;

        private Func<LightProfile, bool> filter;
        private int graceLeft;

        public Collider2D Sensor => sensor;
        public bool IsLit { get; private set; }
        public bool IsExposed { get; private set; }
        public bool GracePaused { get; set; }

        public event Action<LightExposure> ExposureChanged;

        internal Func<LightProfile, bool> Filter => filter ??= Matches;

        protected virtual bool Matches(LightProfile profile) => profile.Illuminates;

        private void Reset() => sensor = GetComponent<Collider2D>();

        private void OnEnable() => ServiceRegistry.Get<ILightService>()?.Register(this);

        private void OnDisable()
        {
            if (ServiceRegistry.TryGet(out ILightService lights)) lights.Unregister(this);
            IsLit = IsExposed = false;
            graceLeft = 0;
        }

        public void RefillGrace(int steps)
        {
            if (useGrace && IsExposed) graceLeft = Mathf.Max(graceLeft, steps);
        }

        internal bool Evaluate(bool lit, int graceSteps)
        {
            bool exposed;
            if (lit)
            {
                graceLeft = useGrace ? graceSteps : 0;
                exposed = true;
            }
            else if (graceLeft > 0)
            {
                if (!GracePaused) graceLeft--;
                exposed = true;
            }
            else exposed = false;

            IsLit = lit;
            bool changed = exposed != IsExposed;
            IsExposed = exposed;
            return changed;
        }

        internal void RaiseChanged() => ExposureChanged?.Invoke(this);

        private void OnDrawGizmos()
        {
            if (sensor == null) return;
            Gizmos.color = !Application.isPlaying ? Color.gray
                : IsLit ? Color.green
                : IsExposed ? new Color(1f, 0.6f, 0f) // in grace
                : Color.red;

            // Drawn from geometry, not bounds: a disabled collider has empty bounds but is still checked.
            var t = sensor.transform;
            Vector3 center = t.TransformPoint(sensor.offset);
            Vector3 scale = t.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), 1f);

            switch (sensor)
            {
                case BoxCollider2D box:
                    Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, t.eulerAngles.z), scale);
                    Gizmos.DrawWireCube(Vector3.zero, box.size);
                    break;
                case CapsuleCollider2D capsule: // drawn as its bounding box
                    Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, t.eulerAngles.z), scale);
                    Gizmos.DrawWireCube(Vector3.zero, capsule.size);
                    break;
                case CircleCollider2D circle:
                    Gizmos.DrawWireSphere(center, circle.radius * Mathf.Max(scale.x, scale.y));
                    break;
                default:
                    var bounds = sensor.bounds;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                    break;
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sensor == null) Debug.LogWarning($"{name}: LightExposure has no sensor collider.", this);
        }
#endif
    }
}