using System;
using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, object> _handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : class
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var existing))
            {
                existing = new List<Action<T>>();
                _handlers[type] = existing;
            }

            var list = (List<Action<T>>)existing;
            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : class
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                var list = (List<Action<T>>)existing;
                list.Remove(handler);

                if (list.Count == 0)
                {
                    _handlers.Remove(type);
                }
            }
        }

        public static void Publish<T>(T eventData) where T : class
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                var list = (List<Action<T>>)existing;
                foreach (var handler in list)
                {
                    try
                    {
                        handler?.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error publishing event {type.Name}: {e}");
                    }
                }
            }
        }

        public static void Clear()
        {
            _handlers.Clear();
        }

        public static void Clear<T>() where T : class
        {
            _handlers.Remove(typeof(T));
        }
    }
}
