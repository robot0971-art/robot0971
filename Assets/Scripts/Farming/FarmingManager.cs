using System.Collections.Generic;
using DI;
using UnityEngine;
using SunnysideIsland.Events;
using SunnysideIsland.Inventory;
using SunnysideIsland.Core;
using SunnysideIsland.Pool;
using SunnysideIsland.GameData;
using Newtonsoft.Json.Linq;
using SunnysideIsland.Farming;

namespace SunnysideIsland.Farming
{
    public class FarmingManager : MonoBehaviour, ISaveable
    {
        public static FarmingManager Instance { get; private set; }

        [Header("=== Settings ===")]
        [SerializeField] public List<FarmPlot> _plots = new List<FarmPlot>();
        [SerializeField] private GameObject _plotPrefab;
        [Header("=== Crop Data ===")]
        [SerializeField] private CropData[] _availableCrops = new CropData[0];

        [Inject(Optional = true)] private IInventorySystem _inventorySystem;
        [Inject] private IPoolManager _poolManager;

        public string SaveKey => "FarmingManager";

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
            if (DIContainer.Global == null)
            {
                DIContainer.InitializeGlobal();
            }
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

            if (Instance == this)
            {
                Instance = null;
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

        public CropData GetCropData(string cropId)
        {
            if (string.IsNullOrEmpty(cropId)) return null;
            
            foreach (var crop in _availableCrops)
            {
                if (crop != null && crop.cropId == cropId)
                    return crop;
            }
            return null;
        }

        public object GetSaveData()
        {
            var dataList = new List<FarmPlotSaveData>();
            foreach (var plot in _plots)
            {
                if (plot != null)
                {
                    var data = plot.GetSaveData() as FarmPlotSaveData;
                    if (data != null)
                    {
                        dataList.Add(data);
                    }
                }
            }
            return dataList;
        }

        public void LoadSaveData(object state)
        {
            var dataList = state as List<FarmPlotSaveData>;
            if (dataList == null && state is JArray jArray)
            {
                dataList = jArray.ToObject<List<FarmPlotSaveData>>();
            }

            if (dataList == null) return;

            string poolName = "FarmPlot";
            for (int i = _plots.Count - 1; i >= 0; i--)
            {
                if (_plots[i] != null)
                {
                    if (_poolManager != null)
                        _poolManager.Despawn(poolName, _plots[i].gameObject);
                    else
                        Destroy(_plots[i].gameObject);
                }
            }
            _plots.Clear();

            foreach (var data in dataList)
            {
                GameObject plotObj = TrySpawnPlotObject(poolName);

                if (plotObj != null)
                {
                    var plot = plotObj.GetComponent<FarmPlot>();
                    if (plot != null)
                    {
                        plot.Clear(); // Reset pool object first
                        plot.LoadSaveData(data);
                        _plots.Add(plot);
                    }
                }
            }
        }

        private GameObject TrySpawnPlotObject(string poolName)
        {
            if (_poolManager != null)
            {
                var pool = _poolManager.GetPool(poolName);
                if (pool != null)
                {
                    GameObject pooledPlot = _poolManager.Spawn(poolName);
                    if (pooledPlot != null)
                    {
                        return pooledPlot;
                    }
                }
            }

            if (_plotPrefab != null)
            {
                return Instantiate(_plotPrefab);
            }

            Debug.LogWarning("[FarmingManager] FarmPlot prefab is missing, cannot restore plots.");
            return null;
        }
    }
}
