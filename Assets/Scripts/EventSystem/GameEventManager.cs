using System;
using System.Collections.Generic;
using UnityEngine;
using DI;
using SunnysideIsland.Events;
using SunnysideIsland.Core;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Events
{
    public enum RandomEventType
    {
        MerchantVisit,
        GoblinRaid,
        TreasureFind,
        Rainbow,
        GhostAppearance,
        BumperCrop,
        Drought
    }

    public enum WeeklyEventType
    {
        None,
        GatheringDay,
        FishingContest,
        MarketDay,
        RestDay
    }

    public enum SeasonalEventType
    {
        None,
        ArborDay,
        BeachFestival,
        HarvestFestival,
        SnowFestival
    }
}

namespace SunnysideIsland.EventSystem
{
    public class GameEventManager : MonoBehaviour, ISaveable
    {
        public static GameEventManager Instance { get; private set; }

        [Header("=== Random Event Settings ===")]
        [SerializeField] private float _merchantVisitChance = 0.1f;
        [SerializeField] private float _goblinRaidChance = 0.05f;
        [SerializeField] private float _treasureFindChance = 0.02f;
        [SerializeField] private float _rainbowChance = 0.01f;
        [SerializeField] private float _ghostAppearanceChance = 0.03f;
        [SerializeField] private float _bumperCropChance = 0.05f;
        [SerializeField] private float _droughtChance = 0.03f;

        [Header("=== References ===")]
        [SerializeField] private GameObject _merchantPrefab;
        [SerializeField] private Transform _merchantSpawnPoint;
        [SerializeField] private GameObject _ghostPrefab;

        [Inject] private TimeManager _timeManager;
        [Inject] private Weather.WeatherSystem _weatherSystem;

        private Events.RandomEventType? _activeRandomEvent;
        private int _activeEventDuration;
        private WeeklyEventType _currentWeeklyEvent;
        private SeasonalEventType _currentSeasonalEvent;

        public Events.RandomEventType? ActiveRandomEvent => _activeRandomEvent;
        public WeeklyEventType CurrentWeeklyEvent => _currentWeeklyEvent;
        public SeasonalEventType CurrentSeasonalEvent => _currentSeasonalEvent;
        public string SaveKey => "GameEventManager";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
        }

        private void OnDayStarted(DayStartedEvent evt)
        {
            CheckWeeklyEvent(evt.Day);
            CheckSeasonalEvent(evt.Day, evt.Season);
            CheckRandomEvent();
        }

        #region Weekly Events

        private void CheckWeeklyEvent(int day)
        {
            int dayOfWeek = (day - 1) % 7;

            _currentWeeklyEvent = dayOfWeek switch
            {
                0 => WeeklyEventType.GatheringDay,
                2 => WeeklyEventType.FishingContest,
                4 => WeeklyEventType.MarketDay,
                6 => WeeklyEventType.RestDay,
                _ => WeeklyEventType.None
            };

            if (_currentWeeklyEvent != WeeklyEventType.None)
            {
                ApplyWeeklyEventEffects(_currentWeeklyEvent);
                
                EventBus.Publish(new WeeklyEventStartedEvent
                {
                    EventType = _currentWeeklyEvent,
                    Day = day
                });
            }
        }

        private void ApplyWeeklyEventEffects(WeeklyEventType eventType)
        {
            switch (eventType)
            {
                case WeeklyEventType.GatheringDay:
                    Debug.Log("[GameEventManager] 채집의 날 - 채집 XP 2배");
                    break;
                case WeeklyEventType.FishingContest:
                    Debug.Log("[GameEventManager] 낚시 대회 - 특별 물고기 출현");
                    break;
                case WeeklyEventType.MarketDay:
                    Debug.Log("[GameEventManager] 시장의 날 - 모든 가격 할인 20%");
                    break;
                case WeeklyEventType.RestDay:
                    Debug.Log("[GameEventManager] 쉬는 날 - 모든 NPC 휴식");
                    break;
            }
        }

        #endregion

        #region Seasonal Events

        private void CheckSeasonalEvent(int day, Season season)
        {
            _currentSeasonalEvent = Events.SeasonalEventType.None;

            int weekNumber = (day - 1) / 7;
            bool isFirstWeek = weekNumber == 0;

            if (isFirstWeek)
            {
                _currentSeasonalEvent = season switch
                {
                    Season.Spring => Events.SeasonalEventType.ArborDay,
                    Season.Summer => Events.SeasonalEventType.BeachFestival,
                    Season.Fall => Events.SeasonalEventType.HarvestFestival,
                    Season.Winter => Events.SeasonalEventType.SnowFestival,
                    _ => Events.SeasonalEventType.None
                };

                if (_currentSeasonalEvent != Events.SeasonalEventType.None)
                {
                    ApplySeasonalEventEffects(_currentSeasonalEvent);
                    
                    EventBus.Publish(new SeasonalEventStartedEvent
                    {
                        EventType = _currentSeasonalEvent,
                        Season = season
                    });
                }
            }
        }

        private void ApplySeasonalEventEffects(Events.SeasonalEventType eventType)
        {
            switch (eventType)
            {
                case Events.SeasonalEventType.ArborDay:
                    Debug.Log("[GameEventManager] 식목일 - 나무 무료 제공");
                    break;
                case Events.SeasonalEventType.BeachFestival:
                    Debug.Log("[GameEventManager] 해변 축제 - 관광객 2배");
                    break;
                case Events.SeasonalEventType.HarvestFestival:
                    Debug.Log("[GameEventManager] 추수제 - 농작물 수확량 2배");
                    break;
                case Events.SeasonalEventType.SnowFestival:
                    Debug.Log("[GameEventManager] 눈축제 - 겨울 한정 아이템");
                    break;
            }
        }

        #endregion

        #region Random Events

        private void CheckRandomEvent()
        {
            if (_activeRandomEvent.HasValue) return;

            float roll = UnityEngine.Random.value;
            float cumulative = 0f;

            cumulative += _merchantVisitChance;
            if (roll < cumulative)
            {
                TriggerRandomEvent(Events.RandomEventType.MerchantVisit);
                return;
            }

            cumulative += _goblinRaidChance;
            if (roll < cumulative)
            {
                TriggerRandomEvent(Events.RandomEventType.GoblinRaid);
                return;
            }

            cumulative += _treasureFindChance;
            if (roll < cumulative)
            {
                TriggerRandomEvent(Events.RandomEventType.TreasureFind);
                return;
            }

            cumulative += _rainbowChance;
            if (roll < cumulative)
            {
                TriggerRandomEvent(Events.RandomEventType.Rainbow);
                return;
            }

            if (_weatherSystem?.CurrentWeather == GameData.WeatherType.Sunny)
            {
                int hour = _timeManager?.CurrentHour ?? 12;
                if (hour >= 21 || hour < 4)
                {
                    cumulative += _ghostAppearanceChance;
                    if (roll < cumulative)
                    {
                        TriggerRandomEvent(Events.RandomEventType.GhostAppearance);
                        return;
                    }
                }
            }

            cumulative += _bumperCropChance;
            if (roll < cumulative)
            {
                TriggerRandomEvent(Events.RandomEventType.BumperCrop);
                return;
            }

            cumulative += _droughtChance;
            if (roll < cumulative)
            {
                TriggerRandomEvent(Events.RandomEventType.Drought);
                return;
            }
        }

        public void TriggerRandomEvent(Events.RandomEventType eventType)
        {
            _activeRandomEvent = eventType;
            _activeEventDuration = GetEventDuration(eventType);

            switch (eventType)
            {
                case Events.RandomEventType.MerchantVisit:
                    SpawnMerchant();
                    break;
                case Events.RandomEventType.GoblinRaid:
                    StartGoblinRaid();
                    break;
                case Events.RandomEventType.TreasureFind:
                    SpawnTreasure();
                    break;
                case Events.RandomEventType.Rainbow:
                    ApplyRainbowEffect();
                    break;
                case Events.RandomEventType.GhostAppearance:
                    SpawnGhost();
                    break;
                case Events.RandomEventType.BumperCrop:
                    ApplyBumperCropEffect();
                    break;
                case Events.RandomEventType.Drought:
                    ApplyDroughtEffect();
                    break;
            }

            EventBus.Publish(new RandomEventStartedEvent
            {
                EventType = eventType,
                Duration = _activeEventDuration
            });

            Debug.Log($"[GameEventManager] Random event started: {eventType}");
        }

        private int GetEventDuration(Events.RandomEventType eventType)
        {
            return eventType switch
            {
                Events.RandomEventType.MerchantVisit => 1,
                Events.RandomEventType.GoblinRaid => 0,
                Events.RandomEventType.TreasureFind => 0,
                Events.RandomEventType.Rainbow => 1,
                Events.RandomEventType.GhostAppearance => 0,
                Events.RandomEventType.BumperCrop => 3,
                Events.RandomEventType.Drought => 3,
                _ => 1
            };
        }

        private void SpawnMerchant()
        {
            if (_merchantPrefab != null && _merchantSpawnPoint != null)
            {
                Instantiate(_merchantPrefab, _merchantSpawnPoint.position, Quaternion.identity);
            }
        }

        private void StartGoblinRaid()
        {
            EventBus.Publish(new GoblinRaidEvent());
        }

        private void SpawnTreasure()
        {
            EventBus.Publish(new TreasureFoundEvent
            {
                TreasureType = UnityEngine.Random.Range(0, 3)
            });
        }

        private void ApplyRainbowEffect()
        {
            if (_weatherSystem != null)
            {
                _weatherSystem.ChangeWeather(GameData.WeatherType.Rainbow);
            }
        }

        private void SpawnGhost()
        {
            if (_ghostPrefab != null)
            {
                Instantiate(_ghostPrefab, transform.position, Quaternion.identity);
            }
        }

        private void ApplyBumperCropEffect()
        {
            Debug.Log("[GameEventManager] 풍작 - 작물 성장 속도 2배");
        }

        private void ApplyDroughtEffect()
        {
            Debug.Log("[GameEventManager] 가뭄 - 물 주기 필요 증가");
        }

        public void EndActiveEvent()
        {
            if (!_activeRandomEvent.HasValue) return;

            var endedEvent = _activeRandomEvent.Value;
            _activeRandomEvent = null;
            _activeEventDuration = 0;

            EventBus.Publish(new RandomEventEndedEvent
            {
                EventType = endedEvent
            });
        }

        #endregion

        #region ISaveable

        public object GetSaveData()
        {
            return new EventManagerSaveData
            {
                ActiveRandomEvent = _activeRandomEvent?.ToString(),
                ActiveEventDuration = _activeEventDuration
            };
        }

        public void LoadSaveData(object data)
        {
            if (data is EventManagerSaveData saveData)
            {
                if (!string.IsNullOrEmpty(saveData.ActiveRandomEvent) && 
                    Enum.TryParse<Events.RandomEventType>(saveData.ActiveRandomEvent, out var eventType))
                {
                    _activeRandomEvent = eventType;
                }
                _activeEventDuration = saveData.ActiveEventDuration;
            }
        }

        [Serializable]
        public class EventManagerSaveData
        {
            public string ActiveRandomEvent;
            public int ActiveEventDuration;
        }

        #endregion
    }

    #region Events

    public class WeeklyEventStartedEvent
    {
        public WeeklyEventType EventType { get; set; }
        public int Day { get; set; }
    }

    public class SeasonalEventStartedEvent
    {
        public SeasonalEventType EventType { get; set; }
        public Season Season { get; set; }
    }

    public class RandomEventStartedEvent
    {
        public Events.RandomEventType EventType { get; set; }
        public int Duration { get; set; }
    }

    public class RandomEventEndedEvent
    {
        public Events.RandomEventType EventType { get; set; }
    }

    public class GoblinRaidEvent { }

    public class TreasureFoundEvent
    {
        public int TreasureType { get; set; }
    }

    #endregion
}