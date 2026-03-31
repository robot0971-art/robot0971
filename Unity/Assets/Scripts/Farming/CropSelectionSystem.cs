using System;
using UnityEngine;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Farming
{
    public interface ICropSelectionSystem
    {
        int SelectedIndex { get; }
        CropData SelectedCrop { get; }
        int SlotCount { get; }
        event Action<int, CropData> OnSelectionChanged;
        void Select(int index);
        CropData GetCropData(int index);
    }

    public class CropSelectionSystem : MonoBehaviour, ICropSelectionSystem
    {
        [Header("=== Crop Slots ===")]
        [SerializeField] private CropData[] _cropDatas = new CropData[5];
        
        private int _selectedIndex = 0;
        
        public int SelectedIndex => _selectedIndex;
        public CropData SelectedCrop => _cropDatas[_selectedIndex];
        public int SlotCount => _cropDatas.Length;
        
        public event Action<int, CropData> OnSelectionChanged;
        
        private void Awake()
        {
            if (_cropDatas == null || _cropDatas.Length == 0)
            {
                _cropDatas = new CropData[5];
            }
        }
        
        private void Update()
        {
            for (int i = 0; i < 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    Select(i);
                }
            }
        }
        
        public void Select(int index)
        {
            if (index < 0 || index >= _cropDatas.Length)
            {
                Debug.LogWarning($"[CropSelectionSystem] Invalid index: {index}");
                return;
            }
            
            if (_cropDatas[index] == null)
            {
                Debug.LogWarning($"[CropSelectionSystem] No crop data at index {index}");
                return;
            }
            
            _selectedIndex = index;
            OnSelectionChanged?.Invoke(index, _cropDatas[index]);
            Debug.Log($"[CropSelectionSystem] Selected: {index} - {_cropDatas[index].cropName}");
        }
        
        public CropData GetCropData(int index)
        {
            if (index >= 0 && index < _cropDatas.Length)
            {
                return _cropDatas[index];
            }
            return null;
        }
    }
}