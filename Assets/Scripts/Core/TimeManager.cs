using System;
using UnityEngine;
using SunnysideIsland.Events;
using Newtonsoft.Json.Linq;

namespace SunnysideIsland.Core
{
    /// <summary>
    /// 게임 ???�간??관리하??매니?�
    /// </summary>
    public class TimeManager : MonoBehaviour, ISaveable
    {
        [Header("=== Time Settings ===")]
        [SerializeField] private float _secondsPerGameMinute = 1f;
        [SerializeField] private int _startDay = 1;
        [SerializeField] private int _startHour = 6;
        [SerializeField] private int _startMinute = 0;

        public string SaveKey => "TimeManager";
        
        [Header("=== Season Settings ===")]
        [SerializeField] private int _daysPerSeason = 7;
        
        [Header("=== Debug ===")]
        [SerializeField] private bool _showDebugInfo = false;
        
        // ?�재 ?�간 ?�태
        public int CurrentDay { get; private set; }
        public int CurrentHour { get; private set; }
        public int CurrentMinute { get; private set; }
        public Season CurrentSeason { get; private set; }
        public TimePhase CurrentTimePhase { get; private set; }
        
        // ?�간 진행 ?�태
        public bool IsPaused { get; private set; }
        public float TimeScale { get; private set; } = 1f;
        
        // ?��? ?�?�머
        private float _elapsedTime;
        private int _previousHour;
        private int _previousDay;
        private TimePhase _previousTimePhase;
        
        // ?�수
        public const int MINUTES_PER_HOUR = 60;
        public const int HOURS_PER_DAY = 24;
        public const int SEASONS_PER_YEAR = 4;
        
        /// <summary>
        /// ?�재 ?�간??�??�위�?반환 (0 ~ 1439)
        /// </summary>
        public int TotalMinutes => CurrentHour * MINUTES_PER_HOUR + CurrentMinute;
        
        /// <summary>
        /// ?�루 �?진행??비율 (0.0 ~ 1.0)
        /// </summary>
        public float TimeOfDay => (float)TotalMinutes / (HOURS_PER_DAY * MINUTES_PER_HOUR);
        
        private void Awake()
        {
            Initialize(_startDay, _startHour, _startMinute);
        }
        
        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
            {
                return;
            }
            // ?�버�? N?�로 ?�음 ?�로 ?�동 (?�시?��? ?�태?�서???�동)
            if (Input.GetKeyDown(KeyCode.N))
            {
                AddDays(1);
                EventBus.Publish(new DebugDaySkippedEvent
                {
                    Day = CurrentDay
                });
                return;
            }
            
            if (IsPaused) return;
            
            UpdateTime();
        }
        
        /// <summary>
        /// ?�간 초기??
        /// </summary>
        public void Initialize(int day, int hour, int minute)
        {
            CurrentDay = day;
            CurrentHour = hour;
            CurrentMinute = minute;
            _previousHour = hour;
            _previousDay = day;
            
            UpdateSeason();
            UpdateTimePhase();
            
            _previousTimePhase = CurrentTimePhase;
            _elapsedTime = 0f;
        }
        
        /// <summary>
        /// ?�간 ?�데?�트
        /// </summary>
        private void UpdateTime()
        {
            _elapsedTime += Time.deltaTime * TimeScale;
            
            if (_elapsedTime >= _secondsPerGameMinute)
            {
                int minutesToAdd = Mathf.FloorToInt(_elapsedTime / _secondsPerGameMinute);
                _elapsedTime -= minutesToAdd * _secondsPerGameMinute;
                
                AddMinutes(minutesToAdd);
            }
        }
        
        /// <summary>
        /// �?추�?
        /// </summary>
        public void AddMinutes(int minutes)
        {
            int totalMinutes = TotalMinutes + minutes;
            int daysToAdd = totalMinutes / (HOURS_PER_DAY * MINUTES_PER_HOUR);
            int remainingMinutes = totalMinutes % (HOURS_PER_DAY * MINUTES_PER_HOUR);
            
            if (daysToAdd > 0)
            {
                CurrentDay += daysToAdd;
                OnDayChanged();
            }
            
            CurrentHour = remainingMinutes / MINUTES_PER_HOUR;
            CurrentMinute = remainingMinutes % MINUTES_PER_HOUR;
            
            OnTimeChanged();
        }
        
        /// <summary>
        /// ?�간 추�?
        /// </summary>
        public void AddHours(int hours)
        {
            AddMinutes(hours * MINUTES_PER_HOUR);
        }
        
        /// <summary>
        /// ??추�?
        /// </summary>
        public void AddDays(int days)
        {
            CurrentDay += days;
            OnDayChanged();
            OnTimeChanged();
        }
        
        /// <summary>
        /// ?�정 ?�간?�로 ?�정
        /// </summary>
        public void SetTime(int day, int hour, int minute)
        {
            int previousDay = CurrentDay;
            
            CurrentDay = day;
            CurrentHour = hour;
            CurrentMinute = minute;
            
            if (previousDay != CurrentDay)
            {
                OnDayChanged();
            }
            
            OnTimeChanged();
        }
        
        /// <summary>
        /// ?�간 변�????�출
        /// </summary>
        private void OnTimeChanged()
        {
            // ?�간 ?�벤??발행
            EventBus.Publish(new TimeChangedEvent
            {
                Day = CurrentDay,
                Hour = CurrentHour,
                Minute = CurrentMinute,
                TimeOfDay = TimeOfDay
            });
            
            // ?�간?� 변�?체크
            UpdateTimePhase();
            if (_previousTimePhase != CurrentTimePhase)
            {
                EventBus.Publish(new TimePhaseChangedEvent
                {
                    PreviousPhase = _previousTimePhase,
                    CurrentPhase = CurrentTimePhase
                });
                _previousTimePhase = CurrentTimePhase;
            }
            
            // ?�각마다 ?�벤??발행
            if (CurrentHour != _previousHour)
            {
                _previousHour = CurrentHour;
            }
            
            if (_showDebugInfo)
            {
            }
        }
        
        /// <summary>
        /// ??변�????�출
        /// </summary>
        private void OnDayChanged()
        {
            if (_previousDay != CurrentDay)
            {
                // ?�전 ??종료 ?�벤??
                EventBus.Publish(new DayEndedEvent
                {
                    Day = _previousDay
                });
                
                // 계절 ?�데?�트
                UpdateSeason();
                
                // ?�로?????�작 ?�벤??
                EventBus.Publish(new DayStartedEvent
                {
                    Day = CurrentDay,
                    Season = CurrentSeason
                });
                
                _previousDay = CurrentDay;
            }
        }
        
        /// <summary>
        /// 계절 ?�데?�트
        /// </summary>
        private void UpdateSeason()
        {
            int seasonIndex = (CurrentDay - 1) / _daysPerSeason % SEASONS_PER_YEAR;
            CurrentSeason = (Season)seasonIndex;
        }
        
        /// <summary>
        /// ?�간?� ?�데?�트
        /// </summary>
        private void UpdateTimePhase()
        {
            int hour = CurrentHour;
            
            if (hour >= 4 && hour < 6)
                CurrentTimePhase = TimePhase.Dawn;
            else if (hour >= 6 && hour < 9)
                CurrentTimePhase = TimePhase.Morning;
            else if (hour >= 9 && hour < 12)
                CurrentTimePhase = TimePhase.Noon;
            else if (hour >= 12 && hour < 14)
                CurrentTimePhase = TimePhase.Afternoon;
            else if (hour >= 14 && hour < 18)
                CurrentTimePhase = TimePhase.Evening;
            else if (hour >= 18 && hour < 21)
                CurrentTimePhase = TimePhase.Dusk;
            else
                CurrentTimePhase = TimePhase.Night;
        }
        
        /// <summary>
        /// ?�간 ?��?
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
            EventBus.Publish(new GamePausedEvent { IsPaused = true });
        }
        
        /// <summary>
        /// ?�간 ?�개
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
            EventBus.Publish(new GamePausedEvent { IsPaused = false });
        }
        
        /// <summary>
        /// ?�간 배율 ?�정
        /// </summary>
        public void SetTimeScale(float scale)
        {
            TimeScale = Mathf.Max(0f, scale);
        }
        
        /// <summary>
        /// ?�?�용 ?�간 ?�이??
        /// </summary>
        [Serializable]
        public class TimeSaveData
        {
            public int Day;
            public int Hour;
            public int Minute;
        }
        
        /// <summary>
        /// ?�???�이??반환
        /// </summary>
        public object GetSaveData()
        {
            return new TimeSaveData
            {
                Day = CurrentDay,
                Hour = CurrentHour,
                Minute = CurrentMinute
            };
        }
        
        /// <summary>
        /// ?�???�이??로드
        /// </summary>
        public void LoadSaveData(object data)
        {
            if (data is TimeSaveData saveData)
            {
                Initialize(saveData.Day, saveData.Hour, saveData.Minute);
                BroadcastCurrentTimeState();
            }
            else if (data is JObject jObject)
            {
                var saveDataFromJson = jObject.ToObject<TimeSaveData>();
                if (saveDataFromJson != null)
                {
                    Initialize(saveDataFromJson.Day, saveDataFromJson.Hour, saveDataFromJson.Minute);
                    BroadcastCurrentTimeState();
                }
            }
        }

        private void BroadcastCurrentTimeState()
        {
            EventBus.Publish(new TimeChangedEvent
            {
                Day = CurrentDay,
                Hour = CurrentHour,
                Minute = CurrentMinute,
                TimeOfDay = TimeOfDay
            });

            EventBus.Publish(new TimePhaseChangedEvent
            {
                PreviousPhase = CurrentTimePhase,
                CurrentPhase = CurrentTimePhase
            });
        }
        
        /// <summary>
        /// ?�재 ?�간 문자??반환 (HH:MM)
        /// </summary>
        public string GetTimeString()
        {
            return $"{CurrentHour:D2}:{CurrentMinute:D2}";
        }
        
        /// <summary>
        /// ?�재 ?�짜 문자??반환 (Day X, Season)
        /// </summary>
        public string GetDateString()
        {
            return $"Day {CurrentDay}, {CurrentSeason}";
        }
    }
}
