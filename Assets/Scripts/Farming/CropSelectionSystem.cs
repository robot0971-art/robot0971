using System;
using DI;
using UnityEngine;
using SunnysideIsland.Events;
using SunnysideIsland.GameData;
using GameDataClass = SunnysideIsland.GameData.GameData;

namespace SunnysideIsland.Farming
{
    public interface ICropSelectionSystem
    {
        int SelectedIndex { get; }
        CropData SelectedCrop { get; }
        int SlotCount { get; }
        event Action<int, CropData, int> OnSlotUpdated;
        event Action<int> OnSelectedIndexChanged;
        void Select(int index);
        void SetSelectedIndex(int index);
        CropData GetCropData(int index);
        int GetCount(int index);
        void AddCrop(CropData cropData, int amount);
        void AddCropByItemId(string cropItemId, int amount);
        bool TryConsume(int index, int amount);
    }

    public class CropSelectionSystem : MonoBehaviour, ICropSelectionSystem
    {
        [Header("=== Crop Slots ===")]
        [SerializeField] private CropData[] _cropDatas = new CropData[5];
        [SerializeField] private int[] _cropCounts = new int[5];

        [Inject(Optional = true)] private GameDataClass _gameData;

        private int _selectedIndex = 0;

        public int SelectedIndex => _selectedIndex;
        public CropData SelectedCrop => _cropDatas[_selectedIndex];
        public int SlotCount => _cropDatas.Length;

        public event Action<int, CropData, int> OnSlotUpdated;
        public event Action<int> OnSelectedIndexChanged;

        private void Awake()
        {
            // 인스펙터 데이터를 보호하기 위해 배열이 없을 때만 새로 생성
            if (_cropDatas == null || _cropDatas.Length == 0)
            {
                _cropDatas = new CropData[5];
            }

            if (_cropCounts == null || _cropCounts.Length != _cropDatas.Length)
            {
                int[] newCounts = new int[_cropDatas.Length];
                if (_cropCounts != null) Array.Copy(_cropCounts, newCounts, Math.Min(_cropCounts.Length, newCounts.Length));
                _cropCounts = newCounts;
            }
        }

        private void Start()
        {
            DIContainer.Inject(this);
            EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
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

            // 아이템이 없어도 선택은 가능하게 하되, 경고만 표시
            _selectedIndex = index;
            OnSelectedIndexChanged?.Invoke(_selectedIndex);
            OnSlotUpdated?.Invoke(index, _cropDatas[index], _cropCounts[index]);
            
            if (_cropDatas[index] != null)
                Debug.Log($"[CropSelectionSystem] Selected: {index} - {_cropDatas[index].cropName} (x{_cropCounts[index]})");
            else
                Debug.Log($"[CropSelectionSystem] Selected Empty Slot: {index}");
        }

        public void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < _cropDatas.Length)
            {
                _selectedIndex = index;
                OnSelectedIndexChanged?.Invoke(_selectedIndex);
                UpdateAllSlotsUI();
            }
        }

        private void UpdateAllSlotsUI()
        {
            for (int i = 0; i < _cropDatas.Length; i++)
            {
                OnSlotUpdated?.Invoke(i, _cropDatas[i], _cropCounts[i]);
            }
        }
        
        public CropData GetCropData(int index)
        {
            if (index >= 0 && index < _cropDatas.Length) return _cropDatas[index];
            return null;
        }
        
        public int GetCount(int index)
        {
            if (index >= 0 && index < _cropCounts.Length) return _cropCounts[index];
            return 0;
        }
        
        public void AddCrop(CropData cropData, int amount)
        {
            if (cropData == null || amount <= 0) return;
            
            int existingIndex = Array.IndexOf(_cropDatas, cropData);
            if (existingIndex >= 0)
            {
                _cropCounts[existingIndex] += amount;
                OnSlotUpdated?.Invoke(existingIndex, cropData, _cropCounts[existingIndex]);
                return;
            }
            
            int emptyIndex = Array.IndexOf(_cropDatas, null);
            if (emptyIndex >= 0)
            {
                _cropDatas[emptyIndex] = cropData;
                _cropCounts[emptyIndex] = amount;
                OnSlotUpdated?.Invoke(emptyIndex, cropData, _cropCounts[emptyIndex]);
                return;
            }
        }
        
        public bool TryConsume(int index, int amount)
        {
            if (index < 0 || index >= _cropCounts.Length) return false;
            if (_cropCounts[index] < amount) return false;
            
            _cropCounts[index] -= amount;
            OnSlotUpdated?.Invoke(index, _cropDatas[index], _cropCounts[index]);
            return true;
        }
        
        public void AddCropByItemId(string cropItemId, int amount)
        {
            if (string.IsNullOrEmpty(cropItemId) || amount <= 0) return;
            
            int index = Array.FindIndex(_cropDatas, x => x != null && x.cropItemId == cropItemId);
            if (index >= 0)
            {
                _cropCounts[index] += amount;
                OnSlotUpdated?.Invoke(index, _cropDatas[index], _cropCounts[index]);
                return;
            }

            CropData cropData = FindCropByItemId(cropItemId);
            if (cropData != null)
            {
                AddCrop(cropData, amount);
            }
        }

        private void OnItemPickedUp(ItemPickedUpEvent evt)
        {
            AddCropByItemId(evt.ItemId, evt.Quantity);
        }

        private CropData FindCropByItemId(string cropItemId)
        {
            if (_gameData?.crops == null)
            {
                return null;
            }

            return _gameData.crops.Find(x => x != null && x.cropItemId == cropItemId);
        }
    }
}
