using DI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SunnysideIsland.Inventory;

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

        [Inject(Optional = true)]
        private IItemConsumptionService _itemConsumptionService;

        private GameData.ItemData _currentItem;

        public GameData.ItemData CurrentItem => _currentItem;

        public void ShowItem(GameData.ItemData item)
        {
            if (item == null)
            {
                return;
            }

            _currentItem = item;
            gameObject.SetActive(true);
            UpdateDisplay();
        }

        public void Hide()
        {
            _currentItem = null;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (_inventorySystem == null)
            {
                DIContainer.TryResolve(out _inventorySystem);
            }

            if (_itemConsumptionService == null)
            {
                DIContainer.TryResolve(out _itemConsumptionService);
            }

            if (_useButton != null)
            {
                _useButton.onClick.AddListener(OnUseButtonClicked);
            }
        }

        private void UpdateDisplay()
        {
            if (_currentItem == null)
            {
                return;
            }

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
                _descriptionText.text = BuildDescription();
            }

            if (_valueText != null)
            {
                _valueText.text = $"{_currentItem.baseValue:N0} G";
            }

            if (_stackText != null)
            {
                _stackText.text = $"Max Stack {_currentItem.maxStack}";
            }

            UpdateUseButton();
        }

        private string BuildDescription()
        {
            string description = _currentItem.description ?? string.Empty;

            if (TryGetRestoreAmounts(out int hungerRestore, out int healthRestore, out float staminaRestore))
            {
                string effects = string.Empty;

                if (hungerRestore > 0)
                {
                    effects += $"Hunger +{hungerRestore}";
                }

                if (healthRestore > 0)
                {
                    effects = string.IsNullOrEmpty(effects)
                        ? $"Health +{healthRestore}"
                        : $"{effects}\nHealth +{healthRestore}";
                }

                if (staminaRestore > 0f)
                {
                    string staminaText = $"Stamina +{staminaRestore:0.#}";
                    effects = string.IsNullOrEmpty(effects)
                        ? staminaText
                        : $"{effects}\n{staminaText}";
                }

                if (!string.IsNullOrEmpty(effects))
                {
                    description = string.IsNullOrEmpty(description)
                        ? effects
                        : $"{description}\n\n{effects}";
                }
            }

            return description;
        }

        private string GetTypeText(GameData.ItemType type)
        {
            return type switch
            {
                GameData.ItemType.Tool => "Tool",
                GameData.ItemType.Consumable => "Consumable",
                GameData.ItemType.Material => "Material",
                GameData.ItemType.Equipment => "Equipment",
                GameData.ItemType.Valuable => "Valuable",
                GameData.ItemType.Quest => "Quest",
                _ => "Unknown"
            };
        }

        private void UpdateUseButton()
        {
            if (_useButton == null)
            {
                return;
            }

            bool canUse = (_currentItem?.itemType == GameData.ItemType.Consumable &&
                           _itemConsumptionService != null &&
                           _itemConsumptionService.CanConsume(_currentItem.itemId)) ||
                          _currentItem?.itemType == GameData.ItemType.Tool;

            _useButton.gameObject.SetActive(canUse);

            if (_useButtonText != null && canUse)
            {
                _useButtonText.text = _currentItem?.itemType == GameData.ItemType.Consumable ? "Use" : "Equip";
            }
        }

        private void OnUseButtonClicked()
        {
            if (_currentItem == null)
            {
                return;
            }

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
            if (_currentItem == null || _itemConsumptionService == null)
            {
                return;
            }

            if (_itemConsumptionService.TryConsume(_currentItem.itemId))
            {
                Hide();
            }
        }

        private void UseTool()
        {
            if (_currentItem == null)
            {
                return;
            }

            Debug.Log($"[ItemDetailPanel] Equip requested for {_currentItem.itemName}");
        }

        private bool TryGetRestoreAmounts(out int hungerRestore, out int healthRestore, out float staminaRestore)
        {
            hungerRestore = 0;
            healthRestore = 0;
            staminaRestore = 0f;

            if (_currentItem == null || _itemConsumptionService == null)
            {
                return false;
            }

            return _itemConsumptionService.TryGetRestoreAmounts(
                _currentItem.itemId,
                out hungerRestore,
                out healthRestore,
                out staminaRestore);
        }
    }
}
