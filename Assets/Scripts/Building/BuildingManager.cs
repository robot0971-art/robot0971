using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Events;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// 건물 관리자
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        [Header("=== Settings ===")]
        [SerializeField] private List<Building> _buildings = new List<Building>();
        
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
            foreach (var building in _buildings)
            {
                if (building != null && 
                    (building.State == BuildingState.Constructing || 
                     building.State == BuildingState.Upgrading))
                {
                    building.ProgressConstruction();
                }
            }
        }
        
        public void RegisterBuilding(Building building)
        {
            if (building != null && !_buildings.Contains(building))
            {
                _buildings.Add(building);
            }
        }
        
        public void UnregisterBuilding(Building building)
        {
            _buildings.Remove(building);
        }
    }
}
