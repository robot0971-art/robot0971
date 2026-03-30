using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DI
{
    public class DIContainer : IDisposable
    {
        private readonly Dictionary<string, object> _registrations = new();
        private readonly Dictionary<string, List<Type>> _interfaceToImplementationMap = new();

        private static readonly List<DIContainer> ContainerStack = new();

        public static DIContainer Global { get; private set; }

        public static void InitializeGlobal()
        {
            if (Global != null)
            {
                Debug.LogWarning("Global DIContainer already initialized");
                return;
            }

            Global = new DIContainer();
            ContainerStack.Add(Global);
        }

        public static DIContainer CreateSceneContainer()
        {
            var container = new DIContainer();
            ContainerStack.Add(container);
            return container;
        }

        public static void RemoveContainer(DIContainer container)
        {
            if (container == Global)
            {
                Debug.LogError("Cannot remove Global container");
                return;
            }

            ContainerStack.Remove(container);
            container.Dispose();
        }

        public static void ClearAll()
        {
            foreach (var container in ContainerStack)
            {
                container.Dispose();
            }
            ContainerStack.Clear();
            Global = null;
        }

        public void Register<TInterface, TImplementation>(string key = "") where TImplementation : TInterface
        {
            var typeKey = GetKey(typeof(TInterface), key);
            _registrations[typeKey] = Activator.CreateInstance<TImplementation>();
        }

        public void RegisterInstance<TInterface>(TInterface instance, string key = "")
        {
            var typeKey = GetKey(typeof(TInterface), key);
            _registrations[typeKey] = instance;
        }

        public void RegisterType<TImplementation>(string key = "")
        {
            var typeKey = GetKey(typeof(TImplementation), key);
            _registrations[typeKey] = Activator.CreateInstance<TImplementation>();
        }

        public static T Resolve<T>(string key = "")
        {
            var typeKey = GetKey(typeof(T), key);

            // 최신 컨테이너부터 역순으로 탐색
            for (int i = ContainerStack.Count - 1; i >= 0; i--)
            {
                if (ContainerStack[i]._registrations.TryGetValue(typeKey, out var instance))
                {
                    return (T)instance;
                }
            }

            throw new KeyNotFoundException($"No registration found for type {typeof(T).FullName} with key '{key}'");
        }

        public static bool TryResolve<T>(out T instance, string key = "")
        {
            var typeKey = GetKey(typeof(T), key);

            for (int i = ContainerStack.Count - 1; i >= 0; i--)
            {
                if (ContainerStack[i]._registrations.TryGetValue(typeKey, out var obj))
                {
                    instance = (T)obj;
                    return true;
                }
            }

            instance = default;
            return false;
        }

        public static void Inject(object target)
        {
            var type = target.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public |
                                       System.Reflection.BindingFlags.NonPublic |
                                       System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                var injectAttr = field.GetCustomAttributes(typeof(InjectAttribute), true).FirstOrDefault();
                if (injectAttr != null)
                {
                    var attr = (InjectAttribute)injectAttr;
                    var injectKey = attr.Key;
                    var instance = ResolveWithFallback(field.FieldType, injectKey, attr.Optional);
                    if (instance != null || !attr.Optional)
                    {
                        field.SetValue(target, instance);
                    }
                }
            }

            var properties = type.GetProperties(System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
                                            System.Reflection.BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var injectAttr = prop.GetCustomAttributes(typeof(InjectAttribute), true).FirstOrDefault();
                if (injectAttr != null && prop.CanWrite)
                {
                    var attr = (InjectAttribute)injectAttr;
                    var injectKey = attr.Key;
                    var instance = ResolveWithFallback(prop.PropertyType, injectKey, attr.Optional);
                    if (instance != null || !attr.Optional)
                    {
                        prop.SetValue(target, instance);
                    }
                }
            }
        }

        private static object ResolveWithFallback(Type type, string key, bool optional = false)
        {
            var typeKey = GetKey(type, key);

            for (int i = ContainerStack.Count - 1; i >= 0; i--)
            {
                if (ContainerStack[i]._registrations.TryGetValue(typeKey, out var instance))
                {
                    return instance;
                }
            }

            if (optional)
            {
                return null;
            }

            throw new KeyNotFoundException($"No registration found for type {type.FullName} with key '{key}'");
        }

        private static string GetKey(Type type, string key)
        {
            return string.IsNullOrEmpty(key) ? type.FullName : $"{type.FullName}:{key}";
        }

        public void Dispose()
        {
            foreach (var registration in _registrations.Values)
            {
                if (registration is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _registrations.Clear();
            _interfaceToImplementationMap.Clear();
        }
    }
}
