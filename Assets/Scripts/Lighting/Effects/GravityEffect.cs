using System;
using UnityEngine;

namespace Lighting
{
    [Serializable]
    public sealed class GravityEffect : LightEffect
    {
        [Tooltip("Multiplies the gravity of anything inside this light. -1 reverses it.")] [SerializeField]
        private float multiplier = -1f;

        public float Multiplier => multiplier;
    }
}