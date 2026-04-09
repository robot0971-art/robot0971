using System.Collections.Generic;
using UnityEngine;
using DI;
using Newtonsoft.Json.Linq;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// Campfire lifecycle and persistence manager.
    /// </summary>
    public class CampfireManager : MonoBehaviour, ISaveable
    {
        public static CampfireManager Instance { get; private set; }

        [Header("=== Settings ===")]
        [SerializeField] private int _maxCampfires = 3;

        [Header("=== References ===")]
        [SerializeField] private TimeManager _timeManager;
        [SerializeField] private Transform _campfireParent;

        [Inject] private BuildingDatabase _buildingDatabase;

        private readonly List<Campfire> _activeCampfires = new List<Campfire>();
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

            if (_timeManager == null)
            {
                _timeManager = FindFirstObjectByType<TimeManager>();
            }
        }

        private void Start()
        {
            DIContainer.Inject(this);
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

        private void OnTimeChanged(TimeChangedEvent evt)
        {
            if (evt.Hour == _previousHour)
            {
                return;
            }

            float hoursPassed = evt.Hour - _previousHour;
            if (hoursPassed < 0)
            {
                hoursPassed += 24f;
            }

            UpdateCampfireTimes(hoursPassed);
            _previousHour = evt.Hour;
        }

        private void UpdateCampfireTimes(float hoursPassed)
        {
            for (int i = _activeCampfires.Count - 1; i >= 0; i--)
            {
                Campfire campfire = _activeCampfires[i];
                if (campfire == null)
                {
                    _activeCampfires.RemoveAt(i);
                    continue;
                }

                campfire.UpdateTime(hoursPassed);
            }
        }

        public void RegisterCampfire(Campfire campfire)
        {
            if (campfire == null || _activeCampfires.Contains(campfire))
            {
                return;
            }

            _activeCampfires.Add(campfire);
            Debug.Log($"[CampfireManager] Campfire registered. Count: {_activeCampfires.Count}/{_maxCampfires}");
        }

        public void UnregisterCampfire(Campfire campfire)
        {
            if (campfire == null)
            {
                return;
            }

            _activeCampfires.Remove(campfire);
            Debug.Log($"[CampfireManager] Campfire unregistered. Count: {_activeCampfires.Count}/{_maxCampfires}");
        }

        public int GetCampfireCount()
        {
            _activeCampfires.RemoveAll(campfire => campfire == null);
            return _activeCampfires.Count;
        }

        public object GetSaveData()
        {
            _activeCampfires.RemoveAll(campfire => campfire == null);

            var saveData = new CampfireManagerSaveData
            {
                MaxCampfires = _maxCampfires,
                CampfireCount = _activeCampfires.Count,
                Campfires = new List<CampfireSaveData>()
            };

            foreach (Campfire campfire in _activeCampfires)
            {
                CampfireSaveData campfireData = campfire.GetSaveData() as CampfireSaveData;
                if (campfireData != null)
                {
                    saveData.Campfires.Add(campfireData);
                }
            }

            return saveData;
        }

        public void LoadSaveData(object state)
        {
            var data = state as CampfireManagerSaveData ?? (state as JObject)?.ToObject<CampfireManagerSaveData>();
            if (data == null)
            {
                return;
            }

            _maxCampfires = data.MaxCampfires;
            RestoreCampfires(data.Campfires);
        }

        public void LoadLegacyCampfires(List<CampfireSaveData> legacyCampfires)
        {
            if (legacyCampfires == null || legacyCampfires.Count == 0)
            {
                return;
            }

            RestoreCampfires(legacyCampfires);
        }

        private void RestoreCampfires(List<CampfireSaveData> campfireDataList)
        {
            ClearExistingCampfires();

            if (campfireDataList == null || campfireDataList.Count == 0)
            {
                return;
            }

            GameObject campfirePrefab = ResolveCampfirePrefab();
            if (campfirePrefab == null)
            {
                Debug.LogError("[CampfireManager] Campfire prefab could not be resolved during load.");
                return;
            }

            foreach (CampfireSaveData campfireData in campfireDataList)
            {
                if (campfireData == null)
                {
                    continue;
                }

                GameObject campfireObject = Instantiate(
                    campfirePrefab,
                    campfireData.Position,
                    Quaternion.identity,
                    _campfireParent);

                if (!campfireObject.TryGetComponent(out Campfire campfire))
                {
                    Debug.LogError("[CampfireManager] Restored campfire prefab is missing the Campfire component.");
                    Destroy(campfireObject);
                    continue;
                }

                campfire.LoadSaveData(campfireData);
                RegisterCampfire(campfire);
            }

            Debug.Log($"[CampfireManager] Restored {campfireDataList.Count} campfires from save data.");
        }

        private void ClearExistingCampfires()
        {
            Campfire[] existingCampfires = FindObjectsByType<Campfire>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Campfire campfire in existingCampfires)
            {
                if (campfire != null && campfire.gameObject.scene.IsValid())
                {
                    Destroy(campfire.gameObject);
                }
            }

            _activeCampfires.Clear();
        }

        private GameObject ResolveCampfirePrefab()
        {
            if (_buildingDatabase == null)
            {
                DIContainer.Inject(this);
            }

            if (_buildingDatabase == null)
            {
                _buildingDatabase = Resources.Load<BuildingDatabase>("BuildingDatabase");
            }

            if (_buildingDatabase == null)
            {
                return null;
            }

            DetailedBuildingData buildingData = _buildingDatabase.GetBuilding("campfire") ?? _buildingDatabase.GetBuilding("Campfire");
            return buildingData?.BuildingPrefab;
        }
    }

    [System.Serializable]
    public class CampfireManagerSaveData
    {
        public int MaxCampfires;
        public int CampfireCount;
        public List<CampfireSaveData> Campfires = new List<CampfireSaveData>();
    }
}
