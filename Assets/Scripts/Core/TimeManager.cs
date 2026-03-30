using System;
using UnityEngine;
using SunnysideIsland.Events;

namespace SunnysideIsland.Core
{
    /// <summary>
    /// 게임 내 시간을 관리하는 매니저
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        [Header("=== Time Settings ===")]
        [SerializeField] private float _secondsPerGameMinute = 1f;
        [SerializeField] private int _startDay = 1;
        [SerializeField] private int _startHour = 6;
        [SerializeField] private int _startMinute = 0;
        
        [Header("=== Season Settings ===")]
        [SerializeField] private int _daysPerSeason = 7;
        
        [Header("=== Debug ===")]
        [SerializeField] private bool _showDebugInfo = false;
        
        // 현재 시간 상태
        public int CurrentDay { get; private set; }
        public int CurrentHour { get; private set; }
        public int CurrentMinute { get; private set; }
        public Season CurrentSeason { get; private set; }
        public TimePhase CurrentTimePhase { get; private set; }
        
        // 시간 진행 상태
        public bool IsPaused { get; private set; }
        public float TimeScale { get; private set; } = 1f;
        
        // 내부 타이머
        private float _elapsedTime;
        private int _previousHour;
        private int _previousDay;
        private TimePhase _previousTimePhase;
        
        // 상수
        public const int MINUTES_PER_HOUR = 60;
        public const int HOURS_PER_DAY = 24;
        public const int SEASONS_PER_YEAR = 4;
        
        /// <summary>
        /// 현재 시간을 분 단위로 반환 (0 ~ 1439)
        /// </summary>
        public int TotalMinutes => CurrentHour * MINUTES_PER_HOUR + CurrentMinute;
        
        /// <summary>
        /// 하루 중 진행된 비율 (0.0 ~ 1.0)
        /// </summary>
        public float TimeOfDay => (float)TotalMinutes / (HOURS_PER_DAY * MINUTES_PER_HOUR);
        
        private void Awake()
        {
            Initialize(_startDay, _startHour, _startMinute);
        }
        
        private void Update()
        {
            // 디버그: N키로 다음 날로 이동 (일시정지 상태에서도 작동)
            if (Input.GetKeyDown(KeyCode.N))
            {
                Debug.Log($"[TimeManager] N키 누름 - Day {CurrentDay} → Day {CurrentDay + 1}");
                AddDays(1);
                return;
            }
            
            if (IsPaused) return;
            
            UpdateTime();
        }
        
        /// <summary>
        /// 시간 초기화
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
        /// 시간 업데이트
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
        /// 분 추가
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
        /// 시간 추가
        /// </summary>
        public void AddHours(int hours)
        {
            AddMinutes(hours * MINUTES_PER_HOUR);
        }
        
        /// <summary>
        /// 일 추가
        /// </summary>
        public void AddDays(int days)
        {
            CurrentDay += days;
            OnDayChanged();
            OnTimeChanged();
        }
        
        /// <summary>
        /// 특정 시간으로 설정
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
        /// 시간 변경 시 호출
        /// </summary>
        private void OnTimeChanged()
        {
            // 시간 이벤트 발행
            EventBus.Publish(new TimeChangedEvent
            {
                Day = CurrentDay,
                Hour = CurrentHour,
                Minute = CurrentMinute,
                TimeOfDay = TimeOfDay
            });
            
            // 시간대 변경 체크
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
            
            // 정각마다 이벤트 발행
            if (CurrentHour != _previousHour)
            {
                _previousHour = CurrentHour;
            }
            
            if (_showDebugInfo)
            {
                Debug.Log($"[TimeManager] Day {CurrentDay}, {CurrentHour:D2}:{CurrentMinute:D2}");
            }
        }
        
        /// <summary>
        /// 일 변경 시 호출
        /// </summary>
        private void OnDayChanged()
        {
            if (_previousDay != CurrentDay)
            {
                // 이전 날 종료 이벤트
                EventBus.Publish(new DayEndedEvent
                {
                    Day = _previousDay
                });
                
                // 계절 업데이트
                UpdateSeason();
                
                // 새로운 날 시작 이벤트
                EventBus.Publish(new DayStartedEvent
                {
                    Day = CurrentDay,
                    Season = CurrentSeason
                });
                
                _previousDay = CurrentDay;
            }
        }
        
        /// <summary>
        /// 계절 업데이트
        /// </summary>
        private void UpdateSeason()
        {
            int seasonIndex = (CurrentDay - 1) / _daysPerSeason % SEASONS_PER_YEAR;
            CurrentSeason = (Season)seasonIndex;
        }
        
        /// <summary>
        /// 시간대 업데이트
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
        /// 시간 정지
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
            EventBus.Publish(new GamePausedEvent { IsPaused = true });
        }
        
        /// <summary>
        /// 시간 재개
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
            EventBus.Publish(new GamePausedEvent { IsPaused = false });
        }
        
        /// <summary>
        /// 시간 배율 설정
        /// </summary>
        public void SetTimeScale(float scale)
        {
            TimeScale = Mathf.Max(0f, scale);
        }
        
        /// <summary>
        /// 저장용 시간 데이터
        /// </summary>
        [Serializable]
        public class TimeSaveData
        {
            public int Day;
            public int Hour;
            public int Minute;
        }
        
        /// <summary>
        /// 저장 데이터 반환
        /// </summary>
        public TimeSaveData GetSaveData()
        {
            return new TimeSaveData
            {
                Day = CurrentDay,
                Hour = CurrentHour,
                Minute = CurrentMinute
            };
        }
        
        /// <summary>
        /// 저장 데이터 로드
        /// </summary>
        public void LoadSaveData(TimeSaveData data)
        {
            if (data != null)
            {
                Initialize(data.Day, data.Hour, data.Minute);
            }
        }
        
        /// <summary>
        /// 현재 시간 문자열 반환 (HH:MM)
        /// </summary>
        public string GetTimeString()
        {
            return $"{CurrentHour:D2}:{CurrentMinute:D2}";
        }
        
        /// <summary>
        /// 현재 날짜 문자열 반환 (Day X, Season)
        /// </summary>
        public string GetDateString()
        {
            return $"Day {CurrentDay}, {CurrentSeason}";
        }
    }
}
