using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Events;
using SunnysideIsland.Inventory;

namespace SunnysideIsland.Farming
{
    public class FarmingManager : MonoBehaviour
    {
        public static FarmingManager Instance { get; private set; }
        
        [Header("=== Settings ===")]
        [SerializeField] public List<FarmPlot> _plots = new List<FarmPlot>();
        [SerializeField] private InventorySystem _inventorySystem;

        private bool _isSubscribed = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[FarmingManager] 중복 인스턴스가 감지되어 삭제됩니다.");
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (Instance != this) return;
            
            // 인벤토리 시스템 찾기
            if (_inventorySystem == null)
            {
                _inventorySystem = FindObjectOfType<InventorySystem>();
            }
            
            if (!_isSubscribed)
            {
                EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
                EventBus.Subscribe<CropHarvestedEvent>(OnCropHarvested);
                _isSubscribed = true;
            }

            if (_plots.Count == 0)
            {
                FindAllPlots();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this && _isSubscribed)
            {
                EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
                EventBus.Unsubscribe<CropHarvestedEvent>(OnCropHarvested);
                _isSubscribed = false;
            }
        }

        private void OnCropHarvested(CropHarvestedEvent evt)
        {
            if (_inventorySystem != null && !string.IsNullOrEmpty(evt.CropId))
            {
                // FarmPlot에서 이미 cropItemId로 처리됨
                bool added = _inventorySystem.AddItem(evt.CropId, evt.Amount);
                Debug.Log($"[FarmingManager] 수확! {evt.CropId} x{evt.Amount}를 인벤토리에 추가 (결과: {added})");
            }
            else
            {
                Debug.LogWarning($"[FarmingManager] 인벤토리 없음({_inventorySystem == null}) 또는 CropId null");
            }
        }

        private void FindAllPlots()
        {
            _plots.Clear();
            _plots.AddRange(FindObjectsOfType<FarmPlot>());
        }

        private void OnDayStarted(DayStartedEvent evt)
        {
            Debug.Log($"[FarmingManager] DayStartedEvent 수신! Day {evt.Day}, Plot 개수: {_plots.Count}");
            foreach (var plot in _plots)
            {
                if (plot != null)
                {
                    plot.DayPassed();
                }
            }
            Debug.Log($"[FarmingManager] 모든 작물 DayPassed 호출 완료");
        }
        public void AdvanceDay()
        {
            foreach (var plot in _plots) // 리스트 이름이 Plots라면 Plots로 수정
            {
                if (plot != null)
                {
                    // 각 밭에 날짜가 지났음을 알림 (FarmPlot에 이 기능이 있어야 함)
                    // 보통 FarmPlot.cs 안에 DayPassed() 같은 함수가 있습니다.
                    plot.DayPassed();
                }
            }
            Debug.Log("모든 작물이 하루만큼 자랐습니다.");
        }

        public void RegisterPlot(FarmPlot plot)
        {
            if (plot != null && !_plots.Contains(plot))
            {
                _plots.Add(plot);
            }
        }

        public void UnregisterPlot(FarmPlot plot)
        {
            _plots.Remove(plot);
        }

        public List<FarmPlot> GetAllPlots()
        {
            return new List<FarmPlot>(_plots);
        }

        public FarmPlot GetPlot(int index)
        {
            if (index < 0 || index >= _plots.Count) return null;
            return _plots[index];
        }
    }
}
