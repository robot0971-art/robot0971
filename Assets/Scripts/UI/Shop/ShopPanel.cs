using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.Inventory;
using GameDataClass = SunnysideIsland.GameData.GameData;

namespace SunnysideIsland.UI.Shop
{
    public enum ShopMode
    {
        Buy,
        Sell
    }

    public class ShopPanel : UIPanel
    {
        [Header("=== Shop Info ===")]
        [SerializeField] private TextMeshProUGUI _shopNameText;
        [SerializeField] private Transform _itemGrid;
        [SerializeField] private GameObject _shopItemPrefab;
        
        [Header("=== Player Info ===")]
        [SerializeField] private TextMeshProUGUI _playerGoldText;
        
        [Header("=== Tabs ===")]
        [SerializeField] private Button _buyTab;
        [SerializeField] private Button _sellTab;
        [SerializeField] private Color _selectedTabColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color _unselectedTabColor = new Color(0.5f, 0.5f, 0.5f);
        
        [Header("=== Quantity Selector ===")]
        [SerializeField] private GameObject _quantitySelectorPanel;
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private Button _quantityDecreaseButton;
        [SerializeField] private Button _quantityIncreaseButton;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _totalPriceText;
        
        [Inject]
        private InventorySystem _inventory;
        [Inject]
        private GameDataClass _gameData;
        
        private ShopMode _currentMode = ShopMode.Buy;
        private string _selectedItemId;
        private int _selectedQuantity = 1;
        private int _selectedPrice;
        private readonly List<GameObject> _shopItems = new List<GameObject>();
        
        protected override void Awake()
        {
            base.Awake();
            _isModal = true;
            
            _buyTab?.onClick.AddListener(() => SwitchMode(ShopMode.Buy));
            _sellTab?.onClick.AddListener(() => SwitchMode(ShopMode.Sell));
            _quantityDecreaseButton?.onClick.AddListener(OnQuantityDecrease);
            _quantityIncreaseButton?.onClick.AddListener(OnQuantityIncrease);
            _confirmButton?.onClick.AddListener(OnConfirmTransaction);
            _cancelButton?.onClick.AddListener(OnCancelTransaction);
        }
        
        protected override void OnOpened()
        {
            base.OnOpened();
            SwitchMode(ShopMode.Buy);
            SubscribeEvents();
        }
        
        protected override void OnClosed()
        {
            base.OnClosed();
            UnsubscribeEvents();
            HideQuantitySelector();
        }
        
        private void SubscribeEvents()
        {
        }
        
        private void UnsubscribeEvents()
        {
        }
        
        public void OpenShop(string shopName)
        {
            if (_shopNameText != null)
            {
                _shopNameText.text = shopName;
            }
            Open();
        }
        
        private void SwitchMode(ShopMode mode)
        {
            _currentMode = mode;
            
            UpdateTabColors();
            RefreshItemList();
        }
        
        private void UpdateTabColors()
        {
            if (_buyTab != null)
            {
                var colors = _buyTab.colors;
                colors.normalColor = _currentMode == ShopMode.Buy ? _selectedTabColor : _unselectedTabColor;
                _buyTab.colors = colors;
            }
            
            if (_sellTab != null)
            {
                var colors = _sellTab.colors;
                colors.normalColor = _currentMode == ShopMode.Sell ? _selectedTabColor : _unselectedTabColor;
                _sellTab.colors = colors;
            }
        }
        
        private void RefreshItemList()
        {
            ClearShopItems();
            
            if (_currentMode == ShopMode.Buy)
            {
                RefreshBuyList();
            }
            else
            {
                RefreshSellList();
            }
        }
        
        private void RefreshBuyList()
        {
            ClearShopItems();
        }
        
        private void RefreshSellList()
        {
            ClearShopItems();
        }
        
        private void CreateShopItem(string itemId, int price, int stock)
        {
            if (_shopItemPrefab == null || _itemGrid == null) return;
            
            var itemGO = Instantiate(_shopItemPrefab, _itemGrid);
            var shopItemUI = itemGO.GetComponent<ShopItemUI>();
            
            if (shopItemUI != null)
            {
                var itemData = _gameData?.GetItem(itemId);
                shopItemUI.Setup(itemId, itemData?.itemName ?? itemId, price, stock);
                shopItemUI.OnClicked += OnShopItemClicked;
            }
            
            _shopItems.Add(itemGO);
        }
        
        private void ClearShopItems()
        {
            foreach (var item in _shopItems)
            {
                if (item != null) Destroy(item);
            }
            _shopItems.Clear();
        }
        
        private void OnShopItemClicked(string itemId, int price)
        {
            _selectedItemId = itemId;
            _selectedPrice = price;
            _selectedQuantity = 1;
            ShowQuantitySelector();
        }
        
        private void ShowQuantitySelector()
        {
            if (_quantitySelectorPanel != null)
            {
                _quantitySelectorPanel.SetActive(true);
            }
            UpdateQuantityDisplay();
        }
        
        private void HideQuantitySelector()
        {
            if (_quantitySelectorPanel != null)
            {
                _quantitySelectorPanel.SetActive(false);
            }
        }
        
        private void UpdateQuantityDisplay()
        {
            if (_quantityText != null)
            {
                _quantityText.text = _selectedQuantity.ToString();
            }
            
            if (_totalPriceText != null)
            {
                int total = _selectedPrice * _selectedQuantity;
                _totalPriceText.text = $"총 {total:N0} G";
            }
        }
        
        private void OnQuantityDecrease()
        {
            if (_selectedQuantity > 1)
            {
                _selectedQuantity--;
                UpdateQuantityDisplay();
            }
        }
        
        private void OnQuantityIncrease()
        {
            _selectedQuantity++;
            UpdateQuantityDisplay();
        }
        
        private void OnConfirmTransaction()
        {
            if (_currentMode == ShopMode.Buy)
            {
                BuyItem();
            }
            else
            {
                SellItem();
            }
            
            HideQuantitySelector();
            RefreshItemList();
        }
        
        private void BuyItem()
        {
            if (string.IsNullOrEmpty(_selectedItemId)) return;
            
            Debug.Log("Shop system disabled");
        }
        
        private void SellItem()
        {
            if (string.IsNullOrEmpty(_selectedItemId)) return;
            
            Debug.Log("Shop system disabled");
        }
        
        private void OnCancelTransaction()
        {
            HideQuantitySelector();
        }
        
        private void UpdatePlayerGold()
        {
            if (_playerGoldText != null)
            {
                _playerGoldText.text = "0 G";
            }
        }
    }
}