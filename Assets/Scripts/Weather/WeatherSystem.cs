using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.GameData;
using SunnysideIsland.Pool;
using Newtonsoft.Json.Linq;

namespace SunnysideIsland.Weather
{
    /// <summary>
    /// ?�씨 ?�스??
    /// 맑음 ???�림 ???�덤(맑음/?�림/�? ?�환
    /// </summary>
    public class WeatherSystem : MonoBehaviour, ISaveable
    {
        [Header("=== Settings ===")]
        [SerializeField] private WeatherType _defaultWeather = WeatherType.Sunny;
        
        [Header("=== Lighting ===")]
        [Tooltip("Global Light 2D (URP) - assign in Inspector")]
        [SerializeField] private Light2D _globalLight;
        
        [Header("=== Lighting Intensity ===")]
        [Tooltip("맑음 ?�태??조명 ?�기")]
        [SerializeField] [Range(0.1f, 2f)] private float _sunnyIntensity = 1.0f;
        
        [Tooltip("?�림 ?�태??조명 ?�기")]
        [SerializeField] [Range(0.1f, 2f)] private float _cloudyIntensity = 0.6f;
        
        [Tooltip("�??�태??조명 ?�기")]
        [SerializeField] [Range(0.1f, 2f)] private float _rainyIntensity = 0.4f;
        
        [Header("=== Weather Duration (seconds) ===")]
        [Tooltip("최소 ?�씨 지???�간 (�?")]
        [SerializeField] private float _minWeatherDuration = 1800f; // 30�?
        
        [Tooltip("최�? ?�씨 지???�간 (�?")]
        [SerializeField] private float _maxWeatherDuration = 7200f; // 2?�간
        
        [Header("=== Rain Effect ===")]
        [Tooltip("Rain effect prefab")]
        [SerializeField] private GameObject _rainEffectPrefab;
        
        [Tooltip("Rain pool initial size")]
        [SerializeField] private int _rainPoolInitialSize = 1;
        
        [Tooltip("Rain pool max size")]
        [SerializeField] private int _rainPoolMaxSize = 3;
        
        [Header("=== Time Phase Lighting ===")]
        [Tooltip("?�벽(4-6?? 조명 ?�기")]
        [SerializeField] [Range(0.1f, 2f)] private float _dawnIntensity = 0.4f;
        
        [Tooltip("�?21-4?? 조명 ?�기")]
        [SerializeField] [Range(0.1f, 2f)] private float _nightIntensity = 0.15f;
        
        [Header("=== Time Phase References ===")]
        [Tooltip("TimeManager 참조")]
        [SerializeField] private TimeManager _timeManager;
        
        // ?�재 ?�간?�
        private TimePhase _currentTimePhase = TimePhase.Morning;
        private bool _isNight = false;
        
        [Header("=== Weather Transition ===")]
        [Tooltip("Cloudy ??Sunny�?�??�률")]
        [SerializeField] [Range(0f, 1f)] private float _cloudyToSunnyChance = 0.4f;
        
        [Tooltip("Cloudy ??Cloudy ?��? ?�률")]
        [SerializeField] [Range(0f, 1f)] private float _cloudyToCloudyChance = 0.3f;
        
        [Tooltip("Cloudy ??Rainy�?�??�률")]
        [SerializeField] [Range(0f, 1f)] private float _cloudyToRainyChance = 0.3f;
        
        private ObjectPool _rainEffectPool;
        private GameObject _activeRainEffect;
        
        public WeatherType CurrentWeather { get; private set; }
        public WeatherType PreviousWeather { get; private set; }
        
        public string SaveKey => "WeatherSystem";
        
        // ?�씨 변�??�?�머
        private float _nextWeatherChangeTime;
        private bool _isWeatherChangePending = false;
        
        private void Awake()
        {
            FindGlobalLight();
            InitializeWeather();
            InitializeRainPool();
        }
        
        private void Start()
        {
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<TimePhaseChangedEvent>(OnTimePhaseChanged);
            
            // TimeManager 찾기
            if (_timeManager == null)
            {
                _timeManager = FindObjectOfType<TimeManager>();
            }
            
            // ?�재 ?�간?� ?�인
            if (_timeManager != null)
            {
                _currentTimePhase = _timeManager.CurrentTimePhase;
                _isNight = (_currentTimePhase == TimePhase.Night);
            }
            
            // 초기 ?�씨 ?�정
            if (CurrentWeather == WeatherType.Sunny && PreviousWeather == WeatherType.Sunny)
            {
                ChangeWeather(_defaultWeather);
                ScheduleNextWeatherChange();
            }
        }
        
        /// <summary>
        /// ?�간?� 변�????�출
        /// </summary>
        private void OnTimePhaseChanged(TimePhaseChangedEvent evt)
        {
            _currentTimePhase = evt.CurrentPhase;
            _isNight = (_currentTimePhase == TimePhase.Night);
            
            // 조명 ?�데?�트
            UpdateLighting();
            
        }
        
        private void Update()
        {
            // ?�간 체크?�여 ?�동 ?�씨 변�?
            if (_isWeatherChangePending && Time.time >= _nextWeatherChangeTime)
            {
                ProgressWeather();
            }
            
            // [TEST] N ?�로 ?�씨 강제 변�?(?�스?�용)
        }
        
        private void OnDestroy()
        {
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Unsubscribe<TimePhaseChangedEvent>(OnTimePhaseChanged);
        }
        
        /// <summary>
        /// Global Light 2D 찾기
        /// </summary>
        private void FindGlobalLight()
        {
            if (_globalLight == null)
            {
                // ?�에??Light2D 찾기 (Global ?�??
                var lights = FindObjectsOfType<Light2D>();
                foreach (var light in lights)
                {
                    if (light.lightType == Light2D.LightType.Global)
                    {
                        _globalLight = light;
                        break;
                    }
                }
                
                if (_globalLight == null)
                {
                    Debug.LogWarning("[WeatherSystem] No Global Light 2D found! Please assign in Inspector.");
                }
            }
        }
        
        /// <summary>
        /// 초기 ?�씨 ?�정
        /// </summary>
        private void InitializeWeather()
        {
            CurrentWeather = _defaultWeather;
            PreviousWeather = _defaultWeather;
            UpdateLighting();
        }
        
        /// <summary>
        /// RainEffect ?� 초기??
        /// </summary>
        private void InitializeRainPool()
        {
            if (_rainEffectPrefab != null)
            {
                _rainEffectPool = new ObjectPool(
                    _rainEffectPrefab, 
                    transform, 
                    _rainPoolInitialSize, 
                    _rainPoolMaxSize, 
                    true, 
                    "RainEffectPool"
                );
            }
        }
        
        /// <summary>
        /// ?�음 ?�씨 변�??�약
        /// </summary>
        private void ScheduleNextWeatherChange()
        {
            float duration = UnityEngine.Random.Range(_minWeatherDuration, _maxWeatherDuration);
            _nextWeatherChangeTime = Time.time + duration;
            _isWeatherChangePending = true;
            
        }
        
        /// <summary>
        /// ?�씨 진행 (Sunny ??Cloudy ??Random)
        /// </summary>
        private void ProgressWeather()
        {
            _isWeatherChangePending = false;
            
            switch (CurrentWeather)
            {
                case WeatherType.Sunny:
                    // 맑음 ???�림
                    ChangeWeather(WeatherType.Cloudy);
                    break;
                    
                case WeatherType.Cloudy:
                    // ?�림 ???�덤
                    WeatherType nextWeather = GetRandomWeatherFromCloudy();
                    ChangeWeather(nextWeather);
                    break;
                    
                case WeatherType.Rainy:
                case WeatherType.Stormy:
                    // �????�림 ?�는 맑음
                    ChangeWeather(UnityEngine.Random.value < 0.5f ? WeatherType.Cloudy : WeatherType.Sunny);
                    break;
                    
                default:
                    ChangeWeather(WeatherType.Sunny);
                    break;
            }
            
            ScheduleNextWeatherChange();
        }
        
        /// <summary>
        /// Cloudy ?�태?�서 ?�음 ?�씨 ?�덤 ?�택
        /// </summary>
        private WeatherType GetRandomWeatherFromCloudy()
        {
            float random = UnityEngine.Random.value;
            float sunnyThreshold = _cloudyToSunnyChance;
            float cloudyThreshold = sunnyThreshold + _cloudyToCloudyChance;
            
            if (random < sunnyThreshold)
            {
                return WeatherType.Sunny;
            }
            else if (random < cloudyThreshold)
            {
                return WeatherType.Cloudy;
            }
            else
            {
                return WeatherType.Rainy;
            }
        }
        
        /// <summary>
        /// ?�씨 변�?
        /// </summary>
        public void ChangeWeather(WeatherType weather)
        {
            if (CurrentWeather == weather) return;
            
            PreviousWeather = CurrentWeather;
            CurrentWeather = weather;
            
            // 조명 ?�데?�트
            UpdateLighting();
            
            // �??�과 ?�데?�트
            UpdateRainEffect();
            
            EventBus.Publish(new WeatherChangedEvent
            {
                PreviousWeather = PreviousWeather,
                CurrentWeather = CurrentWeather
            });
            
        }
        
        /// <summary>
        /// 조명 ?�데?�트 - ?�씨 + ?�간?� 반영
        /// </summary>
        private void UpdateLighting()
        {
            if (_globalLight == null) return;
            
            float targetIntensity = _sunnyIntensity;
            
            // 밤이�?무조�?�?조명
            if (_isNight)
            {
                targetIntensity = _nightIntensity;
            }
            // ?�벽?�면 ?�벽 조명
            else if (_currentTimePhase == TimePhase.Dawn)
            {
                targetIntensity = _dawnIntensity;
            }
            // ??���??�씨???�라
            else
            {
                switch (CurrentWeather)
                {
                    case WeatherType.Sunny:
                    case WeatherType.Rainbow:
                        targetIntensity = _sunnyIntensity;
                        break;
                        
                    case WeatherType.Cloudy:
                        targetIntensity = _cloudyIntensity;
                        break;
                        
                    case WeatherType.Rainy:
                    case WeatherType.Stormy:
                        targetIntensity = _rainyIntensity;
                        break;
                }
            }
            
            _globalLight.intensity = targetIntensity;
        }
        
        /// <summary>
        /// �??�과 ?�데?�트 (Pooling 방식)
        /// </summary>
        private void UpdateRainEffect()
        {
            bool isRainy = CurrentWeather == WeatherType.Rainy || CurrentWeather == WeatherType.Stormy;
            
            if (isRainy)
            {
                // 비�? ?�는???�성?�된 RainEffect가 ?�으�?Pool?�서 가?�오�?
                if (_activeRainEffect == null && _rainEffectPool != null)
                {
                    _activeRainEffect = _rainEffectPool.Get();
                    if (_activeRainEffect != null)
                    {
                        // ?�티???�스???�생
                        var particleSystem = _activeRainEffect.GetComponent<ParticleSystem>();
                        if (particleSystem != null && !particleSystem.isPlaying)
                        {
                            particleSystem.Play();
                        }
                    }
                }
            }
            else
            {
                // 맑을 ?�는 RainEffect�?Pool??반환
                if (_activeRainEffect != null && _rainEffectPool != null)
                {
                    // ?�티??먼�? 중�?
                    var particleSystem = _activeRainEffect.GetComponent<ParticleSystem>();
                    if (particleSystem != null && particleSystem.isPlaying)
                    {
                        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                    
                    // Pool??반환
                    _rainEffectPool.Return(_activeRainEffect);
                    _activeRainEffect = null;
                }
            }
        }
        
        private void OnDayStarted(DayStartedEvent evt)
        {
            // ?�루가 ?�작?�면 ?�씨 변�?가?�성 체크
            // (?��? Update?�서 처리?��?�?추�? ?�업 불필??
        }
        
        /// <summary>
        /// ?�동?�로 ?�씨 변�?(?�스?�용)
        /// </summary>
        [ContextMenu("Force Change to Sunny")]
        private void ForceChangeToSunny()
        {
            ChangeWeather(WeatherType.Sunny);
            ScheduleNextWeatherChange();
        }
        
        [ContextMenu("Force Change to Cloudy")]
        private void ForceChangeToCloudy()
        {
            ChangeWeather(WeatherType.Cloudy);
            ScheduleNextWeatherChange();
        }
        
        [ContextMenu("Force Change to Rainy")]
        private void ForceChangeToRainy()
        {
            ChangeWeather(WeatherType.Rainy);
            ScheduleNextWeatherChange();
        }
        
        public bool IsFishingAllowed()
        {
            return CurrentWeather != WeatherType.Stormy;
        }
        
        public bool IsWateringRequired()
        {
            return CurrentWeather != WeatherType.Rainy && CurrentWeather != WeatherType.Stormy;
        }
        
        public object GetSaveData()
        {
            return new WeatherSaveData
            {
                CurrentWeather = CurrentWeather,
                PreviousWeather = PreviousWeather,
                NextWeatherChangeTime = _nextWeatherChangeTime - Time.time // ?��? ?�간 ?�??
            };
        }
        
        public void LoadSaveData(object state)
        {
            WeatherSaveData data = null;

            if (state is WeatherSaveData weatherData)
            {
                data = weatherData;
            }
            else if (state is JObject jObject)
            {
                data = jObject.ToObject<WeatherSaveData>();
            }

            if (data != null)
            {
                CurrentWeather = data.CurrentWeather;
                PreviousWeather = data.PreviousWeather;
                _nextWeatherChangeTime = Time.time + data.NextWeatherChangeTime;
                _isWeatherChangePending = true;
                
                UpdateLighting();
                UpdateRainEffect();
            }
        }
    }
    
    [Serializable]
    public class WeatherSaveData
    {
        public WeatherType CurrentWeather;
        public WeatherType PreviousWeather;
        public float NextWeatherChangeTime;
    }
    
    /// <summary>
    /// ?�씨 변�??�벤??
    /// </summary>
    public class WeatherChangedEvent
    {
        public WeatherType PreviousWeather { get; set; }
        public WeatherType CurrentWeather { get; set; }
    }
}
