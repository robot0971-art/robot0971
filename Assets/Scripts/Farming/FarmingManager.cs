using System.Collections.Generic;
using DI;
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

        [Inject(Optional = true)] private IInventorySystem _inventorySystem;

        private bool _isSubscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[FarmingManager] Duplicate instance detected");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DIContainer.Global.RegisterInstance(this);
        }

        private void Start()
        {
            if (Instance != this)
            {
                return;
            }

            DIContainer.Inject(this);

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
                bool added = _inventorySystem.AddItem(evt.CropId, evt.Amount);
                Debug.Log($"[FarmingManager] Harvested {evt.CropId} x{evt.Amount} to inventory (result: {added})");
                return;
            }

            Debug.LogWarning($"[FarmingManager] Failed to process crop harvest: item={evt.CropId}, amount={evt.Amount}");
        }

        private void FindAllPlots()
        {
            _plots.Clear();
            _plots.AddRange(FindObjectsOfType<FarmPlot>());
        }

        private void OnDayStarted(DayStartedEvent evt)
        {
            Debug.Log($"[FarmingManager] DayStartedEvent received. Day {evt.Day}, Plot count: {_plots.Count}");
            foreach (var plot in _plots)
            {
                if (plot != null)
                {
                    plot.DayPassed();
                }
            }
        }

        public void AdvanceDay()
        {
            foreach (var plot in _plots)
            {
                if (plot != null)
                {
                    plot.DayPassed();
                }
            }
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
            if (index < 0 || index >= _plots.Count)
            {
                return null;
            }

            return _plots[index];
        }
    }
}
