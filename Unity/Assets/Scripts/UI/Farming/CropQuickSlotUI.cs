using System;
using UnityEngine;
using UnityEngine.UI;
using DI;
using SunnysideIsland.Farming;
using SunnysideIsland.GameData;

namespace SunnysideIsland.UI.Farming
{
    public class CropQuickSlotUI : MonoBehaviour
    {
        [Inject] private ICropSelectionSystem _selectionSystem;
        
        [Header("=== UI Elements ===")]
        [SerializeField] private Image[] _slotBackgrounds;
        [SerializeField] private Image[] _cropIcons;
        [SerializeField] private Text[] _slotNumbers;
        [SerializeField] private Text[] _cropNames;
        
        [Header("=== Colors ===")]
        [SerializeField] private Color _selectedBackgroundColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _normalBackgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        [SerializeField] private Color _emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        
        private void Awake()
        {
            DIContainer.Inject(this);
        }
        
        private void Start()
        {
            UpdateAllSlots();
            
            if (_selectionSystem != null)
            {
                _selectionSystem.OnSelectionChanged += OnSelectionChanged;
            }
        }
        
        private void OnDestroy()
        {
            if (_selectionSystem != null)
            {
                _selectionSystem.OnSelectionChanged -= OnSelectionChanged;
            }
        }
        
        private void OnSelectionChanged(int index, CropData cropData)
        {
            UpdateAllSlots();
        }
        
        private void UpdateAllSlots()
        {
            if (_selectionSystem == null) return;
            
            int slotCount = _selectionSystem.SlotCount;
            
            for (int i = 0; i < slotCount; i++)
            {
                UpdateSlot(i);
            }
        }
        
        private void UpdateSlot(int index)
        {
            CropData cropData = _selectionSystem.GetCropData(index);
            bool isSelected = index == _selectionSystem.SelectedIndex;
            bool hasCrop = cropData != null;
            
            if (_slotBackgrounds != null && index < _slotBackgrounds.Length && _slotBackgrounds[index] != null)
            {
                if (hasCrop)
                {
                    _slotBackgrounds[index].color = isSelected ? _selectedBackgroundColor : _normalBackgroundColor;
                }
                else
                {
                    _slotBackgrounds[index].color = _emptySlotColor;
                }
            }
            
            if (_cropIcons != null && index < _cropIcons.Length && _cropIcons[index] != null)
            {
                if (hasCrop && cropData.growthSprites != null && cropData.growthSprites.Length > 0)
                {
                    _cropIcons[index].sprite = cropData.growthSprites[0];
                    _cropIcons[index].color = Color.white;
                }
                else
                {
                    _cropIcons[index].sprite = null;
                    _cropIcons[index].color = _emptySlotColor;
                }
            }
            
            if (_slotNumbers != null && index < _slotNumbers.Length && _slotNumbers[index] != null)
            {
                _slotNumbers[index].text = (index + 1).ToString();
            }
            
            if (_cropNames != null && index < _cropNames.Length && _cropNames[index] != null)
            {
                if (hasCrop)
                {
                    _cropNames[index].text = cropData.cropName;
                }
                else
                {
                    _cropNames[index].text = "-";
                }
            }
        }
        
        public void SetSelectionSystem(ICropSelectionSystem system)
        {
            if (_selectionSystem != null)
            {
                _selectionSystem.OnSelectionChanged -= OnSelectionChanged;
            }
            
            _selectionSystem = system;
            
            if (_selectionSystem != null)
            {
                _selectionSystem.OnSelectionChanged += OnSelectionChanged;
                UpdateAllSlots();
            }
        }
    }
}