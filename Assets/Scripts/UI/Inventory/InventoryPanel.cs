using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.Events;
using SunnysideIsland.Inventory;
using SunnysideIsland.GameData;
using SunnysideIsland.UI.Components;
using GameDataClass = SunnysideIsland.GameData.GameData;

namespace SunnysideIsland.UI.Inventory
{

    public class InventoryPanel : UIPanel
    {
        [Header("=== Inventory Grid ===")]
        [SerializeField] private Transform _inventoryGrid;
        [SerializeField] private SlotUI _slotPrefab;
        [SerializeField] private int _gridColumns = 8;

        [Header("=== Item Detail ===")]
        [SerializeField] private ItemDetailPanel _itemDetailPanel;

        [Header("=== Info ===")]
        [SerializeField] private TextMeshProUGUI _capacityText;
        [SerializeField] private TextMeshProUGUI _goldText;

        [Header("=== Buttons ===")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _sortButton;

        [SerializeField] private InventorySystem _inventory;
        [SerializeField] private GameDataClass _gameData;
        [SerializeField] private ItemSpriteManager _spriteManager;

        private readonly List<SlotUI> _slots = new List<SlotUI>();
        private int _selectedSlotIndex = -1;

        public int SelectedSlotIndex => _selectedSlotIndex;
        
        protected override void Awake()
        {
            base.Awake();
            
            // DI 대신 직접 찾기
            if (_inventory == null)
                _inventory = FindObjectOfType<InventorySystem>();
            if (_gameData == null)
                _gameData = Resources.Load<GameDataClass>("GameData/GameData");
            if (_inventoryGrid == null)
            {
                var slotGrid = GameObject.Find("SlotGrid");
                if (slotGrid != null)
                    _inventoryGrid = slotGrid.transform;
            }
            
            // 슬롯 초기화
            InitializeSlots();
        }

        protected override async void OnOpened()
        {
            base.OnOpened();
            await RefreshInventoryAsync();
        }

        private void OnEnable()
        {
            _closeButton?.onClick.AddListener(Close);
            _sortButton?.onClick.AddListener(OnSortClicked);
            SubscribeEvents();
            // Refresh는 OnOpened에서만 호출 (중복 방지)
        }

        private void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(Close);
            _sortButton?.onClick.RemoveListener(OnSortClicked);
            UnsubscribeEvents();
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            _selectedSlotIndex = -1;
            HideItemDetail();
        }

        private void SubscribeEvents()
        {
            EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Subscribe<ItemMovedEvent>(OnItemMoved);
        }
        
        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Unsubscribe<ItemMovedEvent>(OnItemMoved);
        }

        private void InitializeSlots()
        {
            // 이미 슬롯이 생성되어 있으면 재생성하지 않음
            if (_slots.Count > 0)
            {
                Debug.Log($"[InventoryPanel] 슬롯 이미 존재함 ({_slots.Count}개), 재생성 건너뜀");
                return;
            }
            
            if (_inventory == null) return;
            if (_slotPrefab == null)
            {
                Debug.LogError("[InventoryPanel] Slot Prefab이 설정되지 않았습니다!");
                return;
            }
            if (_inventoryGrid == null)
            {
                Debug.LogError("[InventoryPanel] Inventory Grid가 설정되지 않았습니다!");
                return;
            }
            
            // 기존 슬롯들 모두 삭제 (Slot 0 포함 모든 자식)
            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    slot.OnClicked -= OnSlotClicked;
                    slot.OnRightClicked -= OnSlotRightClicked;
                    slot.OnHoverEnter -= OnSlotHoverEnter;
                    slot.OnHoverExit -= OnSlotHoverExit;
                }
            }
            _slots.Clear();
            
            // SlotGrid의 모든 자식 오브젝트 삭제
            foreach (Transform child in _inventoryGrid)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
            
            // 프리펩으로부터 슬롯 생성
            for (int i = 0; i < _inventory.Capacity; i++)
            {
                var slotInstance = Instantiate(_slotPrefab, _inventoryGrid);
                slotInstance.SetSlotIndex(i);
                slotInstance.name = $"Slot_{i}";
                
                slotInstance.OnClicked += OnSlotClicked;
                slotInstance.OnRightClicked += OnSlotRightClicked;
                slotInstance.OnHoverEnter += OnSlotHoverEnter;
                slotInstance.OnHoverExit += OnSlotHoverExit;
                
                _slots.Add(slotInstance);
            }
            
            Debug.Log($"[InventoryPanel] {_inventory.Capacity}개의 슬롯 생성 완료");
            UpdateCapacityText();
        }

        public async Task RefreshInventoryAsync()
        {
            if (_inventory == null) 
            {
                Debug.LogWarning("[InventoryPanel] _inventory is null!");
                return;
            }

            // 슬롯이 아직 생성되지 않았으면 먼저 생성
            if (_slots.Count == 0)
            {
                InitializeSlots();
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _inventory.GetSlot(i);
                if (slot != null && !slot.IsEmpty)
                {
                    Sprite icon = await GetItemIconAsync(slot.ItemId);
                    string itemName = GetItemName(slot.ItemId);
                    _slots[i].SetItem(slot.ItemId, itemName, slot.Quantity, icon);
                }
                else
                {
                    _slots[i].Clear();
                }
            }

            UpdateCapacityText();
        }
        
        // 동기 버전 (하위 호환성 유지)
        public void RefreshInventory()
        {
            RefreshInventoryAsync().ConfigureAwait(false);
        }

        private async Task<Sprite> GetItemIconAsync(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            
            if (_spriteManager != null)
            {
                var sprite = _spriteManager.GetSprite(itemId);
                if (sprite != null) return sprite;
            }
            
            // [주석] Addressables 로직 - ItemSpriteManager 우선 사용으로 대체
            /*
            // Fallback: GameData에서 Addressable 아이콘 로드
            if (_gameData != null)
            {
                var itemData = _gameData.GetItem(itemId);
                if (itemData != null)
                {
                    var icon = await itemData.LoadIconAsync();
                    if (icon != null)
                    {
                        Debug.Log($"[InventoryPanel] Loaded icon from Addressables: {itemId}");
                        return icon;
                    }
                }
            }
            */
            
            Debug.LogWarning($"[InventoryPanel] 아이콘을 찾을 수 없음: {itemId}");
            return null;
        }
        
        private string GetItemName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "";
            
            // ItemSpriteManager에서 이름 가져오기
            if (_spriteManager != null)
            {
                var name = _spriteManager.GetItemName(itemId);
                if (!string.IsNullOrEmpty(name) && name != itemId)
                {
                    return name;
                }
            }
            
            // Fallback: GameData에서 이름 가져오기
            if (_gameData != null)
            {
                var itemData = _gameData.GetItem(itemId);
                if (itemData != null && !string.IsNullOrEmpty(itemData.itemName))
                {
                    return itemData.itemName;
                }
            }
            
            // 기본값: itemId 반환
            return itemId;
        }
        
        private void UpdateCapacityText()
        {
            if (_capacityText != null && _inventory != null)
            {
                _capacityText.text = $"{_inventory.UsedSlots} / {_inventory.Capacity}";
            }
        }

        private void OnSlotClicked(SlotUI slot)
        {
            if (_selectedSlotIndex == slot.SlotIndex)
            {
                DeselectSlot();
            }
            else
            {
                SelectSlot(slot.SlotIndex);
            }
        }

        private void OnSlotRightClicked(SlotUI slot)
        {
            if (!slot.IsEmpty)
            {
                ShowItemContextMenu(slot);
            }
        }

        private void OnSlotHoverEnter(SlotUI slot)
        {
            if (!slot.IsEmpty)
            {
                ShowItemDetail(slot.ItemId);
            }
        }

        private void OnSlotHoverExit(SlotUI slot)
        {
            if (_selectedSlotIndex < 0)
            {
                HideItemDetail();
            }
        }

        private void SelectSlot(int index)
        {
            DeselectSlot();

            _selectedSlotIndex = index;
            if (index >= 0 && index < _slots.Count)
            {
                _slots[index].SetSelected(true);
                ShowItemDetail(_slots[index].ItemId);
            }
        }

        private void DeselectSlot()
        {
            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _slots.Count)
            {
                _slots[_selectedSlotIndex].SetSelected(false);
            }
            _selectedSlotIndex = -1;
        }

        private void ShowItemDetail(string itemId)
        {
            if (_itemDetailPanel == null || string.IsNullOrEmpty(itemId)) return;

            var itemData = _gameData?.GetItem(itemId);
            if (itemData != null)
            {
                _itemDetailPanel.ShowItem(itemData);
            }
        }

        private void HideItemDetail()
        {
            _itemDetailPanel?.Hide();
        }

        private void ShowItemContextMenu(SlotUI slot)
        {

        }

        private async void OnSortClicked()
        {
            await RefreshInventoryAsync();
        }

        private async void OnItemPickedUp(ItemPickedUpEvent evt)
        {
            Debug.Log($"[InventoryPanel] OnItemPickedUp: {evt.ItemId} x{evt.Quantity}");
            await RefreshInventoryAsync();
        }

        private async void OnItemMoved(ItemMovedEvent evt)
        {
            await RefreshInventoryAsync();
        }
    }
}