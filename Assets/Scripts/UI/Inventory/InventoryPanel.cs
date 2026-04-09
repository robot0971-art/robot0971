using System.Collections.Generic;
using System.Threading.Tasks;
using DI;
using SunnysideIsland.Events;
using SunnysideIsland.GameData;
using SunnysideIsland.Inventory;
using SunnysideIsland.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameDataClass = SunnysideIsland.GameData.GameData;

namespace SunnysideIsland.UI.Inventory
{
    public class InventoryPanel : UIPanel
    {
        [Header("=== Inventory Grid ===")]
        [SerializeField] private Transform _inventoryGrid;
        [SerializeField] private SlotUI _slotPrefab;
        [SerializeField] private int _slotCount = 20;
        [SerializeField] private int _gridColumns = 8;
        [SerializeField] private Vector2 _slotStartPosition = new Vector2(0f, 0f);
        [SerializeField] private Vector2 _slotSpacing = new Vector2(72f, 72f);
        [SerializeField] private Vector2 _slotSize = new Vector2(64f, 64f);
        [SerializeField] private bool _disableLayoutGroup = true;
        [SerializeField] private bool _useEditorPlacedSlots = true;

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

        [Inject(Optional = true)]
        private IItemConsumptionService _itemConsumptionService;

        private readonly List<SlotUI> _slots = new List<SlotUI>();
        private int _selectedSlotIndex = -1;

        public int SelectedSlotIndex => _selectedSlotIndex;

        protected override void Awake()
        {
            base.Awake();

            if (_inventory == null)
                _inventory = FindObjectOfType<InventorySystem>();
            if (_gameData == null)
                _gameData = Resources.Load<GameDataClass>("GameData/GameData");
            if (_itemConsumptionService == null)
                DIContainer.TryResolve(out _itemConsumptionService);
            if (_inventoryGrid == null)
            {
                var slotGrid = GameObject.Find("SlotGrid");
                if (slotGrid != null)
                    _inventoryGrid = slotGrid.transform;
            }

            ConfigureInventoryGrid();
            InitializeSlots();
        }

        private void OnValidate()
        {
            ConfigureInventoryGrid();
            UpdateSlotLayout();
        }

        protected override async void OnOpened()
        {
            base.OnOpened();
            HideItemDetail();
            await RefreshInventoryAsync();
        }

        private void OnEnable()
        {
            _closeButton?.onClick.AddListener(CloseViaUIManager);
            _sortButton?.onClick.AddListener(OnSortClicked);
            SubscribeEvents();
        }

        private void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(CloseViaUIManager);
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
            EventBus.Subscribe<ItemRemovedEvent>(OnItemRemoved);
            EventBus.Subscribe<ItemMovedEvent>(OnItemMoved);
        }

        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Unsubscribe<ItemRemovedEvent>(OnItemRemoved);
            EventBus.Unsubscribe<ItemMovedEvent>(OnItemMoved);
        }

        private void InitializeSlots()
        {
            if (_slots.Count > 0)
            {
                Debug.Log($"[InventoryPanel] Slots already initialized: {_slots.Count}");
                return;
            }

            if (_inventory == null)
            {
                return;
            }

            if (_slotPrefab == null)
            {
                Debug.LogError("[InventoryPanel] Slot prefab is not assigned.");
                return;
            }

            if (_inventoryGrid == null)
            {
                Debug.LogError("[InventoryPanel] Inventory grid is not assigned.");
                return;
            }

            foreach (var slot in _slots)
            {
                if (slot == null)
                {
                    continue;
                }

                slot.OnClicked -= OnSlotClicked;
                slot.OnRightClicked -= OnSlotRightClicked;
                slot.OnDoubleClicked -= OnSlotDoubleClicked;
                slot.OnHoverEnter -= OnSlotHoverEnter;
                slot.OnHoverExit -= OnSlotHoverExit;
            }

            _slots.Clear();

            var existingSlots = new List<SlotUI>();
            foreach (Transform child in _inventoryGrid)
            {
                if (child.TryGetComponent(out SlotUI existingSlot))
                {
                    existingSlots.Add(existingSlot);
                    continue;
                }

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            if (_useEditorPlacedSlots && existingSlots.Count > 0)
            {
                for (int i = 0; i < existingSlots.Count; i++)
                {
                    var slotInstance = existingSlots[i];
                    slotInstance.gameObject.SetActive(true);
                    slotInstance.SetSlotIndex(i);
                    slotInstance.name = $"Slot_{i}";

                    slotInstance.OnClicked += OnSlotClicked;
                    slotInstance.OnRightClicked += OnSlotRightClicked;
                    slotInstance.OnDoubleClicked += OnSlotDoubleClicked;
                    slotInstance.OnHoverEnter += OnSlotHoverEnter;
                    slotInstance.OnHoverExit += OnSlotHoverExit;

                    _slots.Add(slotInstance);
                }

                Debug.Log($"[InventoryPanel] Using {existingSlots.Count} editor-placed slots.");
                UpdateCapacityText();
                return;
            }

            int visibleSlotCount = GetVisibleSlotCount();
            for (int i = 0; i < visibleSlotCount; i++)
            {
                var slotInstance = Instantiate(_slotPrefab, _inventoryGrid);
                slotInstance.SetSlotIndex(i);
                slotInstance.name = $"Slot_{i}";

                slotInstance.OnClicked += OnSlotClicked;
                slotInstance.OnRightClicked += OnSlotRightClicked;
                slotInstance.OnDoubleClicked += OnSlotDoubleClicked;
                slotInstance.OnHoverEnter += OnSlotHoverEnter;
                slotInstance.OnHoverExit += OnSlotHoverExit;

                _slots.Add(slotInstance);
            }

            UpdateSlotLayout();
            Debug.Log($"[InventoryPanel] Initialized {visibleSlotCount} slots.");
            UpdateCapacityText();
        }

        public async Task RefreshInventoryAsync()
        {
            if (_inventory == null)
            {
                Debug.LogWarning("[InventoryPanel] _inventory is null.");
                return;
            }

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

        public void RefreshInventory()
        {
            RefreshInventoryAsync().ConfigureAwait(false);
        }

        private async Task<Sprite> GetItemIconAsync(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            if (_spriteManager != null)
            {
                var sprite = _spriteManager.GetSprite(itemId);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            Debug.LogWarning($"[InventoryPanel] Could not find icon for item: {itemId}");
            await Task.CompletedTask;
            return null;
        }

        private string GetItemName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return string.Empty;
            }

            if (_spriteManager != null)
            {
                var name = _spriteManager.GetItemName(itemId);
                if (!string.IsNullOrEmpty(name) && name != itemId)
                {
                    return name;
                }
            }

            if (_gameData != null)
            {
                var itemData = _gameData.GetItem(itemId);
                if (itemData != null && !string.IsNullOrEmpty(itemData.itemName))
                {
                    return itemData.itemName;
                }
            }

            return itemId;
        }

        private void UpdateCapacityText()
        {
            if (_capacityText != null && _inventory != null)
            {
                _capacityText.text = $"{_inventory.UsedSlots} / {_slots.Count}";
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

        private async void OnSlotDoubleClicked(SlotUI slot)
        {
            if (slot.IsEmpty || _itemConsumptionService == null)
            {
                return;
            }

            if (_itemConsumptionService.TryConsume(slot.ItemId))
            {
                await RefreshInventoryAsync();
            }
        }

        private void OnSlotHoverEnter(SlotUI slot)
        {
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
            if (_itemDetailPanel == null || string.IsNullOrEmpty(itemId))
            {
                return;
            }

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

        private void CloseViaUIManager()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePanel(this);
            }
            else
            {
                Close();
            }
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

        private async void OnItemRemoved(ItemRemovedEvent evt)
        {
            await RefreshInventoryAsync();
        }

        private void ConfigureInventoryGrid()
        {
            if (_inventoryGrid == null)
            {
                return;
            }

            var layoutGroup = _inventoryGrid.GetComponent<GridLayoutGroup>();
            if (_disableLayoutGroup && layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }
        }

        private int GetVisibleSlotCount()
        {
            return Mathf.Max(1, _slotCount);
        }

        private void UpdateSlotLayout()
        {
            if (_useEditorPlacedSlots)
            {
                return;
            }

            if (_slots.Count == 0)
            {
                return;
            }

            int columns = Mathf.Max(1, _gridColumns);
            for (int i = 0; i < _slots.Count; i++)
            {
                var rectTransform = _slots[i].transform as RectTransform;
                if (rectTransform == null)
                {
                    continue;
                }

                int column = i % columns;
                int row = i / columns;
                Vector2 anchoredPosition = new Vector2(
                    _slotStartPosition.x + (column * _slotSpacing.x),
                    _slotStartPosition.y - (row * _slotSpacing.y));

                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = _slotSize;
            }
        }
    }
}
