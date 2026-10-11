using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public interface IService { }

    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, object> registry = new();
    
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => registry.Clear();

        public static bool Register<T>(T service) where T : class, IService
        {
            if (TryGet<T>(out var existing) && !ReferenceEquals(existing, service))
            {
                Debug.LogWarning($"Core: {typeof(T).Name} already registered, rejecting duplicate.");
                return false;
            }  
            registry[typeof(T)] = service;
            return true;
        }
        public static void Unregister<T>(T service) where T : class, IService
        {
            if (registry.TryGetValue(typeof(T), out var current) && ReferenceEquals(current, service))
                registry.Remove(typeof(T));
        }
        public static bool TryGet<T>(out T service) where T : class, IService
        {
            if (registry.TryGetValue(typeof(T), out var obj) && !(obj is UnityEngine.Object uo && uo == null))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }
        public static T Get<T>() where T : class, IService
        {
            if (TryGet<T>(out var s)) return s;
            Debug.LogError($"Core: no {typeof(T).Name} registered. Is its manager in the scene?");
            return null;
        }
    }
}