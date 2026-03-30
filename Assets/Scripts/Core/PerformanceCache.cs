using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Core
{
    public static class ComponentCache
    {
        private static readonly Dictionary<System.Type, Dictionary<GameObject, Component>> _cache = 
            new Dictionary<System.Type, Dictionary<GameObject, Component>>();
        
        private static readonly int _maxCacheSize = 1000;
        
        public static T GetCachedComponent<T>(GameObject obj) where T : Component
        {
            var type = typeof(T);
            
            if (!_cache.TryGetValue(type, out var typeCache))
            {
                typeCache = new Dictionary<GameObject, Component>();
                _cache[type] = typeCache;
            }
            
            if (typeCache.TryGetValue(obj, out var cached))
            {
                if (cached != null)
                {
                    return cached as T;
                }
                else
                {
                    typeCache.Remove(obj);
                }
            }
            
            var component = obj.GetComponent<T>();
            if (component != null && typeCache.Count < _maxCacheSize)
            {
                typeCache[obj] = component;
            }
            
            return component;
        }
        
        public static void Invalidate(GameObject obj)
        {
            foreach (var typeCache in _cache.Values)
            {
                typeCache.Remove(obj);
            }
        }
        
        public static void Clear()
        {
            foreach (var typeCache in _cache.Values)
            {
                typeCache.Clear();
            }
        }
        
        public static int GetCacheSize()
        {
            int total = 0;
            foreach (var typeCache in _cache.Values)
            {
                total += typeCache.Count;
            }
            return total;
        }
    }
    
    public static class TransformCache
    {
        private static readonly Dictionary<Transform, TransformData> _cache = 
            new Dictionary<Transform, TransformData>();
        
        public struct TransformData
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }
        
        public static void CacheTransform(Transform transform)
        {
            _cache[transform] = new TransformData
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale
            };
        }
        
        public static bool TryGetTransformData(Transform transform, out TransformData data)
        {
            return _cache.TryGetValue(transform, out data);
        }
        
        public static void Clear()
        {
            _cache.Clear();
        }
    }
    
    public static class StringCache
    {
        private static readonly Dictionary<string, string> _internedStrings = 
            new Dictionary<string, string>();
        
        public static string Intern(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            
            if (_internedStrings.TryGetValue(value, out var cached))
            {
                return cached;
            }
            
            _internedStrings[value] = value;
            return value;
        }
        
        public static void Clear()
        {
            _internedStrings.Clear();
        }
    }
}