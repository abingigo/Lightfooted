using System;
using Core;
using UnityEngine;

namespace Lighting
{
    public interface ILightService : IService
    {
        void Register(LightSource source);
        void Register(LightExposure exposure);
        void Unregister(LightSource source);
        void Unregister(LightExposure exposure);

        bool IsLit(Collider2D shape, Func<LightProfile, bool> filter = null);
        bool TryGetEffect<T>(Collider2D shape, out T effect) where T : LightEffect;
    }
}