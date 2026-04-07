using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using Newtonsoft.Json.Linq;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// 모닥불 매니저
    /// 최대 3개 관리 및 시간 업데이트
    /// </summary>
    public class CampfireManager : MonoBehaviour, ISaveable
    {
        public static CampfireManager Instance { get; private set; }
        
        [Header("=== Settings ===")]
        [Tooltip("최대 모닥불 개수")]
        [SerializeField] private int _maxCampfires = 3;
        
        [Header("=== References ===")]
        [SerializeField] private TimeManager _timeManager;
        
        private List<Campfire> _activeCampfires = new List<Campfire>();
        private int _previousHour = -1;
        
        public string SaveKey => "CampfireManager";
        
        public int CampfireCount => _activeCampfires.Count;
        public int MaxCampfires => _maxCampfires;
        public bool CanPlaceMore => _activeCampfires.Count < _maxCampfires;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // TimeManager 찾기
            if (_timeManager == null)
            {
                _timeManager = FindObjectOfType<TimeManager>();
            }
        }
        
        private void Start()
        {
            // 시간 변경 이벤트 구독
            EventBus.Subscribe<TimeChangedEvent>(OnTimeChanged);
        }
        
        private void OnDestroy()
        {
            EventBus.Unsubscribe<TimeChangedEvent>(OnTimeChanged);
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        /// <summary>
        /// 시간 변경 시 호출
        /// </summary>
        private void OnTimeChanged(TimeChangedEvent evt)
        {
            // 시간이 변경될 때마다 Campfire 시간 업데이트
            if (evt.Hour != _previousHour)
            {
                float hoursPassed = evt.Hour - _previousHour;
                if (hoursPassed < 0) hoursPassed += 24; // 날이 넘어갔을 경우
                
                UpdateCampfireTimes(hoursPassed);
                _previousHour = evt.Hour;
            }
        }
        
        /// <summary>
        /// 모든 Campfire 시간 업데이트
        /// </summary>
        private void UpdateCampfireTimes(float hoursPassed)
        {
            for (int i = _activeCampfires.Count - 1; i >= 0; i--)
            {
                var campfire = _activeCampfires[i];
                if (campfire != null)
                {
                    campfire.UpdateTime(hoursPassed);
                }
                else
                {
                    // null 제거
                    _activeCampfires.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Campfire 등록
        /// </summary>
        public void RegisterCampfire(Campfire campfire)
        {
            if (campfire == null) return;
            if (_activeCampfires.Contains(campfire)) return;
            
            _activeCampfires.Add(campfire);
            Debug.Log($"[CampfireManager] Campfire registered. Count: {_activeCampfires.Count}/{_maxCampfires}");
        }
        
        /// <summary>
        /// Campfire 등록 해제
        /// </summary>
        public void UnregisterCampfire(Campfire campfire)
        {
            if (campfire == null) return;
            
            _activeCampfires.Remove(campfire);
            Debug.Log($"[CampfireManager] Campfire unregistered. Count: {_activeCampfires.Count}/{_maxCampfires}");
        }
        
        /// <summary>
        /// 현재 Campfire 개수 반환
        /// </summary>
        public int GetCampfireCount()
        {
            // null 체크
            _activeCampfires.RemoveAll(c => c == null);
            return _activeCampfires.Count;
        }
        
        /// <summary>
        /// 저장 데이터
        /// </summary>
        public object GetSaveData()
        {
            var saveData = new CampfireManagerSaveData
            {
                MaxCampfires = _maxCampfires,
                CampfireCount = _activeCampfires.Count
            };
            
            return saveData;
        }
        
        /// <summary>
        /// 저장 데이터 로드
        /// </summary>
        public void LoadSaveData(object state)
        {
            var data = state as CampfireManagerSaveData ?? (state as JObject)?.ToObject<CampfireManagerSaveData>();
            if (data != null)
            {
                _maxCampfires = data.MaxCampfires;
                // Campfire는 각각의 LoadSaveData에서 로드됨
            }
        }
    }
    
    /// <summary>
    /// CampfireManager 저장 데이터
    /// </summary>
    [System.Serializable]
    public class CampfireManagerSaveData
    {
        public int MaxCampfires;
        public int CampfireCount;
    }
}
