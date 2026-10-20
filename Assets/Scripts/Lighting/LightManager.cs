using System;
using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Lighting
{
    // Runs before other scripts so lights can register in their OnEnable.
    [DefaultExecutionOrder(-1000)]
    public sealed class LightManager : Service<ILightService>, ILightService
    {
        private static readonly Func<LightProfile, bool> Illuminates = profile => profile.Illuminates;

        private readonly Dictionary<Collider2D, LightSource> sourcesByArea = new();
        private readonly List<Collider2D> hits = new();
        private ContactFilter2D lightFilter;

        [Tooltip("Physics steps an exposure with grace stays exposed after leaving light (coyote time).")]
        [SerializeField, Min(0)]
        private int graceSteps = 6;

        [Tooltip(
            "Copy moved Transforms into physics before checking. Needed while lights move by Transform instead of Rigidbody2D.")]
        [SerializeField]
        private bool syncTransforms = true;

        private readonly List<LightExposure> exposures = new();
        private readonly List<LightExposure> changed = new();

        protected override void Awake()
        {
            base.Awake();
            var layer = LayerMask.NameToLayer(LightSource.LayerName);
            if (layer < 0) Debug.LogError($"LightManager: no '{LightSource.LayerName}' layer exists.", this);

            lightFilter = new ContactFilter2D { useTriggers = true };
            lightFilter.SetLayerMask(layer >= 0 ? 1 << layer : 0);
        }

        public void Register(LightSource source)
        {
            foreach (var area in source.Areas)
            {
                if (area == null) continue;
                if (sourcesByArea.TryGetValue(area, out var owner) && owner != source)
                {
                    Debug.LogWarning($"LightManager: area '{area.name}' is already owned by '{owner.name}'.", source);
                    continue;
                }

                sourcesByArea[area] = source;
            }
        }

        public void Unregister(LightSource source)
        {
            foreach (var area in source.Areas)
            {
                if (area is null) continue; // real null only: a destroyed area must still be removed
                if (sourcesByArea.TryGetValue(area, out var owner) && owner == source) sourcesByArea.Remove(area);
            }
        }

        public void Register(LightExposure exposure)
        {
            if (!exposures.Contains(exposure)) exposures.Add(exposure);
        }

        public void Unregister(LightExposure exposure) => exposures.Remove(exposure);

        private void FixedUpdate()
        {
            if (syncTransforms) Physics2D.SyncTransforms();

            changed.Clear();
            foreach (var exposure in exposures)
                if (exposure.Evaluate(IsLit(exposure.Sensor, exposure.Filter), graceSteps))
                    changed.Add(exposure);

            // Events go out after all checks, so every listener sees this step's final state.
            foreach (var exposure in changed)
                if (exposure.isActiveAndEnabled) exposure.RaiseChanged();
        }

        public bool IsLit(Collider2D shape, Func<LightProfile, bool> filter = null)
        {
            if (!shape || ShapeQuery.Overlap(shape, lightFilter, hits) == 0) return false;
            filter ??= Illuminates;
            foreach (var hit in hits)
                if (sourcesByArea.TryGetValue(hit, out var source) && filter(source.Profile))
                    return true;
            return false;
        }
        protected override void OnDestroy()
        {
            sourcesByArea.Clear();
            exposures.Clear();
            changed.Clear();
            base.OnDestroy();
        }
    }
}