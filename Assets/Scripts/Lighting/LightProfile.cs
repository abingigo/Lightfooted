using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
    [Serializable]
    public sealed class LightProfile
    {
        [SerializeField] private bool illuminates = true;
        [SerializeReference] private List<LightEffect> effects = new();

        public bool Illuminates => illuminates;

        public bool TryGet<T>(out T effect) where T : LightEffect
        {
            foreach (var candidate in effects)
            {
                if (candidate is T match)
                {
                    effect = match;
                    return true;
                }
            }

            effect = null;
            return false;
        }

        public bool Has<T>() where T : LightEffect => TryGet<T>(out _);
    }
}