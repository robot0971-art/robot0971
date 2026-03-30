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
        
        [Inject]
        private HealthSystem _healthSystem;
        [Inject]
        private HungerSystem _hungerSystem;
        [Inject]
        private StaminaSystem _staminaSystem;
        [Inject]
        private TimeManager _timeManager;
        [Inject]
        private Weather.WeatherSystem _weatherSystem;
        [Inject]
        private InventorySystem _inventorySystem;
        [Inject]
        private CraftingSystem _craftingSystem;
        
        private const int RequiredWood = 50;
        private const float UPDATE_INTERVAL = 1f;
        private bool _gameClear = false;
        private float _updateTimer = 0f;
        
        private void CreateDefaultUI()
        {
            // Create wood count text if not assigned
            if (_woodCountText == null)
            {
                // Find existing Canvas
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    GameObject canvasGO = new GameObject("HUDCanvas");
                    canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 10;
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                }
                
                GameObject textGO = new GameObject("WoodCountText");
                textGO.transform.SetParent(canvas.transform, false);
                _woodCountText = textGO.AddComponent<TextMeshProUGUI>();
                _woodCountText.text = "";
                _woodCountText.fontSize = 24;
                _woodCountText.color = Color.white;
                _woodCountText.alignment = TextAlignmentOptions.TopLeft;
                RectTransform rect = _woodCountText.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(10, -10);
                rect.sizeDelta = new Vector2(400, 50);
            }
        }
        
        private void Start()
        {
            SubscribeEvents();
            if (_gameClearPanel != null)
                _gameClearPanel.SetActive(false);
            CreateDefaultUI();
            UpdateQuestGoal();
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        
        private void Update()
        {
            if (_gameClear) return;
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= UPDATE_INTERVAL)
            {
                _updateTimer = 0f;
                UpdateQuestGoal();
            }
            
            // CraftingPanel 열기 단축키 (C)
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
            if (_woodCountText == null) return;
            int currentWood = _inventorySystem != null ? _inventorySystem.CountItem("wood") : 0;
            _woodCountText.text = $"목표: 나무 {RequiredWood}개 모아 배 만들기 (현재: {currentWood}/{RequiredWood})";
            if (currentWood >= RequiredWood && !_gameClear)
            {
                // Optionally indicate ready to craft
            }
        }
        
        private void OnItemCrafted(ItemCraftedEvent evt)
        {
            if (evt.ResultItemId == "boat")
            {
                // Boat crafted, game clear will be triggered by GameManager, but we can also update UI
                UpdateQuestGoal();
            }
            else
            {
                UpdateQuestGoal();
            }
        }
        
        private void OnItemPickedUp(ItemPickedUpEvent evt)
        {
            UpdateQuestGoal();
        }
        
        private void OnGameClear(GameManager.GameClearEvent evt)
        {
            _gameClear = true;
            if (_woodCountText != null)
                _woodCountText.text = "축하합니다! 배를 만들어 섬을 탈출했습니다!";
            
            if (_gameClearPanel == null)
            {
                // Create simple game clear UI
                GameObject canvasGO = new GameObject("GameClearCanvas");
                Canvas canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                
                // Panel
                GameObject panelGO = new GameObject("Panel");
                panelGO.transform.SetParent(canvasGO.transform, false);
                Image panelImage = panelGO.AddComponent<Image>();
                panelImage.color = new Color(0, 0, 0, 0.8f);
                RectTransform panelRect = panelGO.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.sizeDelta = Vector2.zero;
                
                // Text
                GameObject textGO = new GameObject("Text");
                textGO.transform.SetParent(panelGO.transform, false);
                TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
                text.text = "게임 클리어!\n섬을 탈출했습니다!";
                text.fontSize = 48;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                RectTransform textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.1f, 0.3f);
                textRect.anchorMax = new Vector2(0.9f, 0.7f);
                textRect.sizeDelta = Vector2.zero;
                
                // Button
                GameObject buttonGO = new GameObject("Button");
                buttonGO.transform.SetParent(panelGO.transform, false);
                Button button = buttonGO.AddComponent<Button>();
                Image buttonImage = buttonGO.AddComponent<Image>();
                buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);
                RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.4f, 0.15f);
                buttonRect.anchorMax = new Vector2(0.6f, 0.25f);
                buttonRect.sizeDelta = Vector2.zero;
                
                GameObject buttonTextGO = new GameObject("ButtonText");
                buttonTextGO.transform.SetParent(buttonGO.transform, false);
                TextMeshProUGUI buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
                buttonText.text = "메인 메뉴로 돌아가기";
                buttonText.fontSize = 24;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.color = Color.white;
                RectTransform btTextRect = buttonTextGO.GetComponent<RectTransform>();
                btTextRect.anchorMin = Vector2.zero;
                btTextRect.anchorMax = Vector2.one;
                btTextRect.sizeDelta = Vector2.zero;
                
                button.onClick.AddListener(() => GameManager.Instance.ReturnToMainMenu());
                
                _gameClearPanel = canvasGO;
            }
            else
            {
                _gameClearPanel.SetActive(true);
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
                GameDataWeatherType.Sunny => "맑음",
                GameDataWeatherType.Cloudy => "흐림",
                GameDataWeatherType.Rainy => "비",
                GameDataWeatherType.Stormy => "폭풍",
                GameDataWeatherType.Rainbow => "무지개",
                _ => ""
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
            if (string.IsNullOrEmpty(itemId)) return "";
            
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