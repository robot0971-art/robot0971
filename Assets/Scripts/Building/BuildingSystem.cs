using UnityEngine;
using SunnysideIsland.Events;
using SunnysideIsland.Inventory;
using SunnysideIsland.UI.Building;

namespace SunnysideIsland.Building
{
    public class BuildingSystem : MonoBehaviour
    {
        [Header("=== References ===")]
        [SerializeField] private BuildingPlacer _placer;
        [SerializeField] private BuildingDatabase _buildingDatabase;
        [SerializeField] private BuildingManager _buildingManager;
        [SerializeField] private InventorySystem _inventorySystem;

        [Header("=== UI ===")]
        [SerializeField] private BuildingPanel _buildingPanel;

        public bool IsInBuildMode => _placer != null && _placer.IsInBuildMode;

        public DetailedBuildingData GetBuildingData(string buildingId)
        {
            return _buildingDatabase?.GetBuilding(buildingId);
        }

        private void Start()
        {
            EventBus.Subscribe<BuildingPlacementStartedEvent>(OnPlacementStarted);
            EventBus.Subscribe<ConstructionCancelledEvent>(OnConstructionCancelled);
            EventBus.Subscribe<BuildingPlaceConfirmEvent>(OnBuildingPlaceConfirm);
            EventBus.Subscribe<BuildingPlacementCancelledEvent>(OnPlacementCancelled);

            if (_placer != null)
            {
                _placer.Initialize(this, _buildingManager);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<BuildingPlacementStartedEvent>(OnPlacementStarted);
            EventBus.Unsubscribe<ConstructionCancelledEvent>(OnConstructionCancelled);
            EventBus.Unsubscribe<BuildingPlaceConfirmEvent>(OnBuildingPlaceConfirm);
            EventBus.Unsubscribe<BuildingPlacementCancelledEvent>(OnPlacementCancelled);
        }

        public void ToggleBuildMode()
        {
            if (_buildingPanel == null) return;

            if (_buildingPanel.IsOpen)
            {
                _buildingPanel.Close();
            }
            else
            {
                _buildingPanel.Open();
            }
        }

        public void EnterBuildMode(string buildingId)
        {
            if (_placer == null || _buildingDatabase == null) return;

            var buildingData = _buildingDatabase.GetBuilding(buildingId);
            if (buildingData == null)
            {
                Debug.LogWarning($"[BuildingSystem] Building not found: {buildingId}");
                return;
            }

            _placer.StartPlacement(buildingData);
        }

        public void ExitBuildMode()
        {
            if (_placer != null)
            {
                _placer.CancelPlacement();
            }
        }

        public bool CanAfford(DetailedBuildingData buildingData)
        {
            if (_inventorySystem == null || buildingData?.Cost == null) return false;

            for (int i = 0; i < buildingData.Cost.Materials.Count; i++)
            {
                string material = buildingData.Cost.Materials[i];
                int required = buildingData.Cost.Amounts[i];
                int current = _inventorySystem.CountItem(material.ToLower());

                if (current < required)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetRequiredWood(DetailedBuildingData buildingData)
        {
            if (buildingData?.Cost == null) return 0;

            for (int i = 0; i < buildingData.Cost.Materials.Count; i++)
            {
                if (buildingData.Cost.Materials[i].ToLower() == "wood")
                {
                    return buildingData.Cost.Amounts[i];
                }
            }

            return 0;
        }

        public void SpendResources(DetailedBuildingData buildingData)
        {
            if (_inventorySystem == null || buildingData?.Cost == null) return;

            for (int i = 0; i < buildingData.Cost.Materials.Count; i++)
            {
                string material = buildingData.Cost.Materials[i].ToLower();
                int amount = buildingData.Cost.Amounts[i];
                _inventorySystem.RemoveItem(material, amount);
            }
        }

        public void RefundResources(DetailedBuildingData buildingData)
        {
            if (_inventorySystem == null || buildingData?.Cost == null) return;

            for (int i = 0; i < buildingData.Cost.Materials.Count; i++)
            {
                string material = buildingData.Cost.Materials[i].ToLower();
                int amount = buildingData.Cost.Amounts[i];
                _inventorySystem.AddItem(material, amount);
            }
        }

        private void OnPlacementStarted(BuildingPlacementStartedEvent evt)
        {
            EnterBuildMode(evt.BuildingId);
        }

        private void OnConstructionCancelled(ConstructionCancelledEvent evt)
        {
            var buildingData = _buildingDatabase?.GetBuilding(evt.BuildingId);
            if (buildingData != null)
            {
                RefundResources(buildingData);
            }
        }

        private void OnBuildingPlaceConfirm(BuildingPlaceConfirmEvent evt)
        {
            var buildingData = _buildingDatabase?.GetBuilding(evt.BuildingId);
            if (buildingData != null && _placer != null)
            {
                SpendResources(buildingData);
                _placer.ConfirmBuildingPlacement(buildingData, evt.GridPosition);
            }
        }

        private void OnPlacementCancelled(BuildingPlacementCancelledEvent evt)
        {
            _buildingPanel?.Open();
        }

        public void OnBuildingPlaced(string buildingId, Vector3Int gridPosition)
        {
            EventBus.Publish(new BuildingPlacedEvent
            {
                BuildingId = buildingId,
                Position = new Vector3(gridPosition.x, gridPosition.y, 0)
            });
        }
    }
}