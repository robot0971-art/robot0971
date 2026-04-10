using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.Events;
using SunnysideIsland.Survival;
using SunnysideIsland.Core;
using SunnysideIsland.UI.Components;
using SunnysideIsland.Weather;
using SunnysideIsland.Inventory;
using SunnysideIsland.Crafting;
using GameDataWeatherType = SunnysideIsland.GameData.WeatherType;
using GameDataClass = SunnysideIsland.GameData.GameData;

namespace SunnysideIsland.UI.HUD
{
    public class HUDPanel : UIPanel
    {
        [Header("=== Stat Bars ===")]
        [SerializeField] private StatBar _healthBar;
        [SerializeField] private StatBar _hungerBar;
        [SerializeField] private StatBar _staminaBar;

        [Header("=== Info Displays ===")]
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _dayText;
        [SerializeField] private Image _weatherIcon;
        [SerializeField] private TextMeshProUGUI _weatherText;

        [Header("=== Quick Slots ===")]
        [SerializeField] private SlotUI[] _quickSlots;

        [Header("=== Game Data ===")]
        [SerializeField] private GameDataClass _gameData;

        [Header("=== Notifications ===")]
        [SerializeField] private NotificationArea _notificationArea;

        [Header("=== Weather Icons ===")]
        [SerializeField] private Sprite _sunnyIcon;
        [SerializeField] private Sprite _cloudyIcon;
        [SerializeField] private Sprite _rainyIcon;
        [SerializeField] private Sprite _stormyIcon;
        [SerializeField] private Sprite _rainbowIcon;

        [Header("=== Wood Progress ===")]
        [SerializeField] private TextMeshProUGUI _woodCountText;
        [SerializeField] private GameObject _gameClearPanel;

        [Inject(Optional = true)]
        private HealthSystem _healthSystem;

        [Inject(Optional = true)]
        private HungerSystem _hungerSystem;

        [Inject(Optional = true)]
        private StaminaSystem _staminaSystem;

        [Inject(Optional = true)]
        private TimeManager _timeManager;

        [Inject(Optional = true)]
        private Weather.WeatherSystem _weatherSystem;

        [Inject(Optional = true)]
        private InventorySystem _inventorySystem;

        [Inject(Optional = true)]
        private CraftingSystem _craftingSystem;

        private const int RequiredWood = 50;
        private const float UPDATE_INTERVAL = 1f;

        private bool _gameClear;
        private float _updateTimer;
        private string _woodCountTemplate;

        private void Start()
        {
            SubscribeEvents();

            if (_gameClearPanel != null)
            {
                _gameClearPanel.SetActive(false);
            }

            if (_woodCountText != null)
            {
                _woodCountTemplate = _woodCountText.text;
            }

            RefreshAllStats();
            UpdateQuestGoal();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
            {
                return;
            }

            if (UIManager.Instance != null
                && UIManager.Instance.GetPanel<SunnysideIsland.UI.Menu.BoatConfirmPanel>()?.IsOpen == true)
            {
                return;
            }

            if (_gameClear)
            {
                return;
            }

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= UPDATE_INTERVAL)
            {
                _updateTimer = 0f;
                UpdateQuestGoal();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                UIManager.Instance?.OpenCraftingPanel();
            }
        }

        private void SubscribeEvents()
        {
            EventBus.Subscribe<HealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<HungerChangedEvent>(OnHungerChanged);
            EventBus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
            EventBus.Subscribe<TimeChangedEvent>(OnTimeChanged);
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
            EventBus.Subscribe<ItemCraftedEvent>(OnItemCrafted);
            EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Subscribe<GameManager.GameClearEvent>(OnGameClear);
        }

        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<HealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<HungerChangedEvent>(OnHungerChanged);
            EventBus.Unsubscribe<StaminaChangedEvent>(OnStaminaChanged);
            EventBus.Unsubscribe<TimeChangedEvent>(OnTimeChanged);
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Unsubscribe<WeatherChangedEvent>(OnWeatherChanged);
            EventBus.Unsubscribe<ItemCraftedEvent>(OnItemCrafted);
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Unsubscribe<GameManager.GameClearEvent>(OnGameClear);
        }

        private void UpdateQuestGoal()
        {
            if (_woodCountText == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_woodCountTemplate))
            {
                _woodCountTemplate = _woodCountText.text;
            }

            int currentWood = _inventorySystem != null ? _inventorySystem.CountItem("wood") : 0;
            _woodCountText.text = _woodCountTemplate
                .Replace("{current}", currentWood.ToString())
                .Replace("{max}", RequiredWood.ToString());
        }

        private void OnItemCrafted(ItemCraftedEvent evt)
        {
            UpdateQuestGoal();
        }

        private void OnItemPickedUp(ItemPickedUpEvent evt)
        {
            UpdateQuestGoal();
        }

        private void OnGameClear(GameManager.GameClearEvent evt)
        {
            _gameClear = true;

            if (_gameClearPanel != null)
            {
                _gameClearPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[HUDPanel] Game clear panel is not assigned.");
            }
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            RefreshAllStats();
        }

        private void RefreshAllStats()
        {
            if (_healthSystem != null)
            {
                _healthBar?.SetValue(_healthSystem.CurrentValue, _healthSystem.MaxValue);
            }

            if (_hungerSystem != null)
            {
                _hungerBar?.SetValue(_hungerSystem.CurrentValue, _hungerSystem.MaxValue);
            }

            if (_staminaSystem != null)
            {
                _staminaBar?.SetValue(_staminaSystem.CurrentValue, _staminaSystem.MaxValue);
            }

            if (_timeManager != null)
            {
                UpdateTimeDisplay();
                UpdateDayDisplay();
            }

            if (_weatherSystem != null)
            {
                UpdateWeatherDisplay(_weatherSystem.CurrentWeather);
            }
        }

        private void OnHealthChanged(HealthChangedEvent evt)
        {
            _healthBar?.SetValue(evt.CurrentHealth, evt.MaxHealth);
        }

        private void OnHungerChanged(HungerChangedEvent evt)
        {
            _hungerBar?.SetValue(evt.CurrentHunger, evt.MaxHunger);
        }

        private void OnStaminaChanged(StaminaChangedEvent evt)
        {
            _staminaBar?.SetValue(evt.CurrentStamina, evt.MaxStamina);
        }

        private void OnTimeChanged(TimeChangedEvent evt)
        {
            UpdateTimeDisplay();
        }

        private void OnDayStarted(DayStartedEvent evt)
        {
            UpdateDayDisplay();
        }

        private void OnWeatherChanged(WeatherChangedEvent evt)
        {
            UpdateWeatherDisplay(evt.CurrentWeather);
        }

        private void UpdateTimeDisplay()
        {
            if (_timeText != null && _timeManager != null)
            {
                _timeText.text = _timeManager.GetTimeString();
            }
        }

        private void UpdateDayDisplay()
        {
            if (_dayText != null && _timeManager != null)
            {
                _dayText.text = $"Day {_timeManager.CurrentDay}";
            }
        }

        private void UpdateWeatherDisplay(GameDataWeatherType weather)
        {
            if (_weatherIcon != null)
            {
                _weatherIcon.sprite = GetWeatherIcon(weather);
            }

            if (_weatherText != null)
            {
                _weatherText.text = GetWeatherName(weather);
            }
        }

        private Sprite GetWeatherIcon(GameDataWeatherType weather)
        {
            return weather switch
            {
                GameDataWeatherType.Sunny => _sunnyIcon,
                GameDataWeatherType.Cloudy => _cloudyIcon,
                GameDataWeatherType.Rainy => _rainyIcon,
                GameDataWeatherType.Stormy => _stormyIcon,
                GameDataWeatherType.Rainbow => _rainbowIcon,
                _ => _sunnyIcon
            };
        }

        private string GetWeatherName(GameDataWeatherType weather)
        {
            return weather switch
            {
                GameDataWeatherType.Sunny => "Sunny",
                GameDataWeatherType.Cloudy => "Cloudy",
                GameDataWeatherType.Rainy => "Rainy",
                GameDataWeatherType.Stormy => "Stormy",
                GameDataWeatherType.Rainbow => "Rainbow",
                _ => string.Empty
            };
        }

        public void ShowNotification(string message, float duration = 3f)
        {
            _notificationArea?.ShowNotification(message, duration);
        }

        public void ShowNotification(string message, Color color, float duration = 3f)
        {
            _notificationArea?.ShowNotification(message, color, duration);
        }

        public void UpdateQuickSlot(int index, string itemId, int quantity, Sprite icon)
        {
            if (index >= 0 && index < _quickSlots.Length)
            {
                string itemName = GetItemName(itemId);
                _quickSlots[index].SetItem(itemId, itemName, quantity, icon);
            }
        }

        private string GetItemName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return string.Empty;
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

        public void ClearQuickSlot(int index)
        {
            if (index >= 0 && index < _quickSlots.Length)
            {
                _quickSlots[index].Clear();
            }
        }
    }
}
