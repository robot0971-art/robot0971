using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SunnysideIsland.Building;

namespace SunnysideIsland.UI.Building
{
    public class BuildingDetailPanel : MonoBehaviour
    {
        [Header("=== Building Info ===")]
        [SerializeField] private Image _buildingPreview;
        [SerializeField] private TextMeshProUGUI _buildingNameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _buildTimeText;
        
        [Header("=== Effects ===")]
        [SerializeField] private Transform _effectsContainer;
        [SerializeField] private GameObject _effectItemPrefab;
        
        [Header("=== Costs ===")]
        [SerializeField] private Transform _costListContainer;
        [SerializeField] private GameObject _costItemPrefab;
        
        [Header("=== Requirements ===")]
        [SerializeField] private GameObject _requirementsPanel;
        [SerializeField] private TextMeshProUGUI _requirementsText;
        
        private DetailedBuildingData _currentBuilding;
        
        public DetailedBuildingData CurrentBuilding => _currentBuilding;
        
        public void ShowBuilding(DetailedBuildingData building)
        {
            if (building == null) return;
            
            _currentBuilding = building;
            gameObject.SetActive(true);
            
            UpdateDisplay();
        }
        
        public void Hide()
        {
            _currentBuilding = null;
            gameObject.SetActive(false);
        }
        
        private void UpdateDisplay()
        {
            if (_currentBuilding == null) return;
            
            if (_buildingNameText != null)
            {
                _buildingNameText.text = _currentBuilding.BuildingName;
            }
            
            if (_descriptionText != null)
            {
                _descriptionText.text = _currentBuilding.Description;
            }
            
            if (_buildTimeText != null)
            {
                if (_currentBuilding.BuildTime > 0)
                {
                    _buildTimeText.text = $"건설 시간: {_currentBuilding.BuildTime}일";
                }
                else
                {
                    _buildTimeText.text = "즉시 건설";
                }
            }
            
            UpdateEffects();
            UpdateCosts();
            UpdateRequirements();
        }
        
        private void UpdateEffects()
        {
            if (_effectsContainer == null) return;
            
            foreach (Transform child in _effectsContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (_currentBuilding.Effects == null) return;
            
            foreach (var effect in _currentBuilding.Effects)
            {
                var item = Instantiate(_effectItemPrefab, _effectsContainer);
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = effect.Description;
                }
            }
        }
        
        private void UpdateCosts()
        {
            if (_costListContainer == null) return;
            
            foreach (Transform child in _costListContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (_currentBuilding.Cost == null) return;
            
            if (_currentBuilding.Cost.Gold > 0)
            {
                AddCostItem("골드", _currentBuilding.Cost.Gold);
            }
            
            if (_currentBuilding.Cost.Materials != null)
            {
                for (int i = 0; i < _currentBuilding.Cost.Materials.Count; i++)
                {
                    string material = _currentBuilding.Cost.Materials[i];
                    int amount = i < _currentBuilding.Cost.Amounts.Count ? _currentBuilding.Cost.Amounts[i] : 0;
                    AddCostItem(material, amount);
                }
            }
        }
        
        private void AddCostItem(string name, int amount)
        {
            if (_costItemPrefab == null || _costListContainer == null) return;
            
            var item = Instantiate(_costItemPrefab, _costListContainer);
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{name}: {amount}";
            }
        }
        
        private void UpdateRequirements()
        {
            if (_requirementsPanel == null) return;
            
            bool hasRequirements = _currentBuilding.UnlocksAtQuest != null && 
                                   _currentBuilding.UnlocksAtQuest.Length > 0;
            
            _requirementsPanel.SetActive(hasRequirements);
            
            if (hasRequirements && _requirementsText != null)
            {
                _requirementsText.text = $"퀘스트 완료 필요: {_currentBuilding.UnlocksAtQuest[0]}";
            }
        }
    }
}