using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.Inventory;
using SunnysideIsland.Survival;

namespace SunnysideIsland.UI.Inventory
{
    public class ItemDetailPanel : MonoBehaviour
    {
        [Header("=== Item Info ===")]
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private TextMeshProUGUI _itemTypeText;
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        
        [Header("=== Stats ===")]
        [SerializeField] private GameObject _statsPanel;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private TextMeshProUGUI _stackText;
        
        [Header("=== Actions ===")]
        [SerializeField] private Button _useButton;
        [SerializeField] private Button _dropButton;
        [SerializeField] private TextMeshProUGUI _useButtonText;
        
        [Header("=== References ===")]
        [SerializeField] private InventorySystem _inventorySystem;
        [SerializeField] private HungerSystem _hungerSystem;
        
        private GameData.ItemData _currentItem;
        
        public GameData.ItemData CurrentItem => _currentItem;
        
        public void ShowItem(GameData.ItemData item)
        {
            if (item == null) return;
            
            _currentItem = item;
            gameObject.SetActive(true);
            
            UpdateDisplay();
        }
        
        public void Hide()
        {
            _currentItem = null;
            gameObject.SetActive(false);
        }
        
        private void UpdateDisplay()
        {
            if (_currentItem == null) return;
            
            if (_itemNameText != null)
            {
                _itemNameText.text = _currentItem.itemName;
            }
            
            if (_itemTypeText != null)
            {
                _itemTypeText.text = GetTypeText(_currentItem.itemType);
            }
            
            if (_descriptionText != null)
            {
                _descriptionText.text = _currentItem.description;
            }
            
            if (_valueText != null)
            {
                _valueText.text = $"{_currentItem.baseValue:N0} G";
            }
            
            if (_stackText != null)
            {
                _stackText.text = $"최대 {_currentItem.maxStack}개";
            }
            
            UpdateUseButton();
        }
        
        private string GetTypeText(GameData.ItemType type)
        {
            return type switch
            {
                GameData.ItemType.Tool => "도구",
                GameData.ItemType.Consumable => "소모품",
                GameData.ItemType.Material => "재료",
                GameData.ItemType.Equipment => "장비",
                GameData.ItemType.Valuable => "귀중품",
                GameData.ItemType.Quest => "퀘스트 아이템",
                _ => "기타"
            };
        }
        
        private void Awake()
        {
            if (_inventorySystem == null)
                _inventorySystem = DIContainer.Resolve<InventorySystem>();
            if (_hungerSystem == null)
                _hungerSystem = DIContainer.Resolve<HungerSystem>();
            
            if (_useButton != null)
                _useButton.onClick.AddListener(OnUseButtonClicked);
        }
        
        private void UpdateUseButton()
        {
            if (_useButton == null) return;
            
            bool canUse = _currentItem?.itemType == GameData.ItemType.Consumable || 
                          _currentItem?.itemType == GameData.ItemType.Tool;
            
            _useButton.gameObject.SetActive(canUse);
            
            if (_useButtonText != null && canUse)
            {
                _useButtonText.text = _currentItem?.itemType == GameData.ItemType.Consumable ? "사용" : "장착";
            }
        }
        
        private void OnUseButtonClicked()
        {
            if (_currentItem == null) return;
            
            switch (_currentItem.itemType)
            {
                case GameData.ItemType.Consumable:
                    UseConsumable();
                    break;
                case GameData.ItemType.Tool:
                    UseTool();
                    break;
            }
        }
        
        private void UseConsumable()
        {
            if (_currentItem == null || _inventorySystem == null) return;
            
            // 소모품 효과 결정 (하드코딩, 추후 데이터 기반으로 변경 가능)
            int hungerRestore = GetHungerRestoreAmount(_currentItem.itemId);
            
            if (hungerRestore > 0 && _hungerSystem != null)
            {
                _hungerSystem.Modify(hungerRestore);
                Debug.Log($"[ItemDetailPanel] {_currentItem.itemName} 사용: 허기 {hungerRestore} 회복");
            }
            
            // 인벤토리에서 아이템 제거 (1개)
            _inventorySystem.RemoveItem(_currentItem.itemId, 1);
            
            // 인벤토리 업데이트를 위한 이벤트 발행 (이미 InventorySystem에서 발행함)
            
            // 패널 닫기
            Hide();
        }
        
        private void UseTool()
        {
            // 도구 장착 기능 (현재는 단순 로그)
            Debug.Log($"[ItemDetailPanel] {_currentItem.itemName} 장착");
            // EquipmentSystem이 제거되었으므로 아무 효과 없음
        }
        
        private int GetHungerRestoreAmount(string itemId)
        {
            // 간단한 맵핑 (나중에 ItemData에 효과 필드 추가 시 변경)
            return itemId switch
            {
                "apple" => 20,
                "bread" => 30,
                "cooked_fish" => 40,
                "berry" => 10,
                _ => 0
            };
        }
    }
}