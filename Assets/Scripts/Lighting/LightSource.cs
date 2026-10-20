using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core;

namespace Lighting
{
    [DisallowMultipleComponent]
    public sealed class LightSource : MonoBehaviour
    {
        public const string LayerName = "Light";

        [Tooltip("Trigger colliders that define where this light shines. Must be on the Light layer.")] [SerializeField]
        private Collider2D[] areas = { };

        [Tooltip("Optional. When set, overrides the local profile below (for lights that share settings).")]
        [SerializeField]
        private LightProfileAsset sharedProfile;

        [SerializeField] private LightProfile profile = new();

        public IReadOnlyList<Collider2D> Areas => areas;
        public LightProfile Profile => sharedProfile != null ? sharedProfile.Profile : profile;

        private void Reset() =>
            areas = GetComponentsInChildren<Collider2D>(true).Where(c => c.isTrigger).ToArray();

        private void OnEnable() => ServiceRegistry.Get<ILightService>()?.Register(this);

        private void OnDisable()
        {
            if (ServiceRegistry.TryGet(out ILightService lights)) lights.Unregister(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Profile.Illuminates ? new Color(1f, 0.9f, 0.3f, 0.8f) : new Color(0.4f, 0.8f, 1f, 0.8f);
            foreach (var area in areas)
            {
                if (area == null || !area.enabled || !area.gameObject.activeInHierarchy) continue;
                var bounds = area.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            int layer = LayerMask.NameToLayer(LayerName);
            if (layer < 0)
            {
                Debug.LogWarning($"LightSource: no '{LayerName}' layer exists. Add it in Tags and Layers.", this);
                return;
            }

            foreach (var area in areas)
                if (area != null && (!area.isTrigger || area.gameObject.layer != layer))
                    Debug.LogWarning($"{name}: light area '{area.name}' must be a trigger on the {LayerName} layer.",
                        this);
        }
#endif
    }
}