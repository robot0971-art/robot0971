using System;
using System.Collections.Generic;
using UnityEngine;
using DI;
using SunnysideIsland.Core;

namespace SunnysideIsland.Analytics
{
    public interface IAnalyticsService
    {
        void TrackEvent(string eventName, Dictionary<string, object> parameters = null);
        void TrackProgress(string progressName, int value);
        void TrackResource(string resourceId, int amount);
        void TrackQuest(string questId, string action);
        void TrackBuilding(string buildingId, string action);
        void TrackCombat(string enemyType, string action, int damage);
        void Flush();
    }

    public class AnalyticsEvent
    {
        public string EventName;
        public Dictionary<string, object> Parameters;
        public DateTime Timestamp;
    }

    public class AnalyticsManager : MonoBehaviour, IAnalyticsService, ISaveable
    {
        public static AnalyticsManager Instance { get; private set; }
        
        [Header("=== Settings ===")]
        [SerializeField] private bool _isEnabled = true;
        [SerializeField] private int _maxQueueSize = 100;
        [SerializeField] private float _flushInterval = 60f;
        [SerializeField] private bool _debugLog = false;
        
        private readonly Queue<AnalyticsEvent> _eventQueue = new Queue<AnalyticsEvent>();
        private readonly Dictionary<string, int> _progressData = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _resourceTotals = new Dictionary<string, int>();
        
        private float _flushTimer;
        private int _sessionCount;
        private float _sessionStartTime;
        
        public string SaveKey => "AnalyticsManager";
        public bool IsEnabled => _isEnabled;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            _sessionCount++;
            _sessionStartTime = Time.realtimeSinceStartup;
            
            TrackEvent("session_start", new Dictionary<string, object>
            {
                { "session_count", _sessionCount },
                { "platform", Application.platform.ToString() },
                { "version", Application.version }
            });
        }
        
        private void Update()
        {
            if (_flushInterval > 0)
            {
                _flushTimer += Time.deltaTime;
                if (_flushTimer >= _flushInterval)
                {
                    Flush();
                    _flushTimer = 0f;
                }
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TrackSessionEnd();
                Flush();
            }
        }
        
        private void OnApplicationQuit()
        {
            TrackSessionEnd();
            Flush();
        }
        
        private void TrackSessionEnd()
        {
            float sessionDuration = Time.realtimeSinceStartup - _sessionStartTime;
            
            TrackEvent("session_end", new Dictionary<string, object>
            {
                { "duration_seconds", Mathf.RoundToInt(sessionDuration) },
                { "events_count", _eventQueue.Count }
            });
        }
        
        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_isEnabled) return;
            
            var analyticsEvent = new AnalyticsEvent
            {
                EventName = eventName,
                Parameters = parameters ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };
            
            _eventQueue.Enqueue(analyticsEvent);
            
            if (_eventQueue.Count > _maxQueueSize)
            {
                _eventQueue.Dequeue();
            }
            
            if (_debugLog)
            {
                Debug.Log($"[Analytics] {eventName}: {FormatParameters(parameters)}");
            }
        }
        
        public void TrackProgress(string progressName, int value)
        {
            if (!_isEnabled) return;
            
            _progressData[progressName] = value;
            
            TrackEvent("progress", new Dictionary<string, object>
            {
                { "name", progressName },
                { "value", value }
            });
        }
        
        public void TrackResource(string resourceId, int amount)
        {
            if (!_isEnabled) return;
            
            if (!_resourceTotals.ContainsKey(resourceId))
            {
                _resourceTotals[resourceId] = 0;
            }
            _resourceTotals[resourceId] += amount;
            
            TrackEvent("resource", new Dictionary<string, object>
            {
                { "resource_id", resourceId },
                { "amount", amount },
                { "total", _resourceTotals[resourceId] }
            });
        }
        
        public void TrackQuest(string questId, string action)
        {
            TrackEvent("quest", new Dictionary<string, object>
            {
                { "quest_id", questId },
                { "action", action }
            });
        }
        
        public void TrackBuilding(string buildingId, string action)
        {
            TrackEvent("building", new Dictionary<string, object>
            {
                { "building_id", buildingId },
                { "action", action }
            });
        }
        
        public void TrackCombat(string enemyType, string action, int damage)
        {
            TrackEvent("combat", new Dictionary<string, object>
            {
                { "enemy_type", enemyType },
                { "action", action },
                { "damage", damage }
            });
        }
        
        public void Flush()
        {
            if (_eventQueue.Count == 0) return;
            
            var events = new List<AnalyticsEvent>();
            while (_eventQueue.Count > 0)
            {
                events.Add(_eventQueue.Dequeue());
            }
            
            if (_debugLog)
            {
                Debug.Log($"[Analytics] Flushing {events.Count} events");
            }
        }
        
        public Dictionary<string, int> GetProgressData()
        {
            return new Dictionary<string, int>(_progressData);
        }
        
        public int GetProgressValue(string progressName)
        {
            return _progressData.TryGetValue(progressName, out var value) ? value : 0;
        }
        
        public Dictionary<string, int> GetResourceTotals()
        {
            return new Dictionary<string, int>(_resourceTotals);
        }
        
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }
        
        private string FormatParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return "{}";
            
            var sb = new System.Text.StringBuilder();
            sb.Append("{ ");
            bool first = true;
            foreach (var kvp in parameters)
            {
                if (!first) sb.Append(", ");
                sb.Append($"{kvp.Key}: {kvp.Value}");
                first = false;
            }
            sb.Append(" }");
            return sb.ToString();
        }
        
        public object GetSaveData()
        {
            return new AnalyticsSaveData
            {
                SessionCount = _sessionCount,
                ProgressData = new Dictionary<string, int>(_progressData),
                ResourceTotals = new Dictionary<string, int>(_resourceTotals)
            };
        }
        
        public void LoadSaveData(object state)
        {
            if (state is AnalyticsSaveData data)
            {
                _sessionCount = data.SessionCount;
                
                _progressData.Clear();
                foreach (var kvp in data.ProgressData)
                {
                    _progressData[kvp.Key] = kvp.Value;
                }
                
                _resourceTotals.Clear();
                foreach (var kvp in data.ResourceTotals)
                {
                    _resourceTotals[kvp.Key] = kvp.Value;
                }
            }
        }
    }
    
    [Serializable]
    public class AnalyticsSaveData
    {
        public int SessionCount;
        public Dictionary<string, int> ProgressData;
        public Dictionary<string, int> ResourceTotals;
    }
}