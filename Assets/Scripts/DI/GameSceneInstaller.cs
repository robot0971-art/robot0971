using UnityEngine;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Inventory;
using SunnysideIsland.Building;
using SunnysideIsland.Farming;
using SunnysideIsland.Fishing;
using SunnysideIsland.Weather;
using SunnysideIsland.Quest;
using SunnysideIsland.Crafting;
using SunnysideIsland.Cooking;

using SunnysideIsland.Survival;
using SunnysideIsland.Tutorial;
using SunnysideIsland.UI;
using SunnysideIsland.GameData;
using SunnysideIsland.Localization;

namespace DI
{
    public class GameSceneInstaller : SceneInstaller
    {
        [Header("=== Core Systems ===")]
        [SerializeField] private TimeManager _timeManager;
        [SerializeField] private SaveSystem _saveSystem;
        [SerializeField] private GameManager _gameManager;
        
        [Header("=== Survival Systems ===")]
        [SerializeField] private HealthSystem _healthSystem;
        [SerializeField] private HungerSystem _hungerSystem;
        [SerializeField] private StaminaSystem _staminaSystem;
        
        [Header("=== Production Systems ===")]
        [SerializeField] private InventorySystem _inventorySystem;
        [SerializeField] private FarmingManager _farmingManager;
        [SerializeField] private CropSelectionSystem _cropSelectionSystem;
        [SerializeField] private FishingSystem _fishingSystem;
        [SerializeField] private Grid _grid;
        
        [Header("=== Building Systems ===")]
        [SerializeField] private BuildingManager _buildingManager;
        [SerializeField] private BuildingSystem _buildingSystem;
        [SerializeField] private BuildingDatabase _buildingDatabase;
        
        [Header("=== Content Systems ===")]
        [SerializeField] private QuestSystem _questSystem;
        [SerializeField] private CraftingSystem _craftingSystem;
        [SerializeField] private CookingSystem _cookingSystem;

        
        [Header("=== Environment ===")]
        [SerializeField] private WeatherSystem _weatherSystem;
        
        [Header("=== Support Systems ===")]
        [SerializeField] private TutorialManager _tutorialManager;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private ItemSpriteManager _itemSpriteManager;
        [SerializeField] private LocalizationManager _localizationManager;
        
        [Header("=== Shared Data ===")]
        [SerializeField] private GameData _gameData;
        
        protected override void InstallSceneBindings()
        {
            if (_timeManager != null) Container.RegisterInstance(_timeManager);
            if (_saveSystem != null) Container.RegisterInstance(_saveSystem);
            if (_gameManager != null) Container.RegisterInstance(_gameManager);
            
            if (_healthSystem != null) Container.RegisterInstance(_healthSystem);
            if (_hungerSystem != null) Container.RegisterInstance(_hungerSystem);
            if (_staminaSystem != null) Container.RegisterInstance(_staminaSystem);
            
            if (_inventorySystem != null)
            {
                Container.RegisterInstance(_inventorySystem);
                Container.RegisterInstance<IInventorySystem>(_inventorySystem);
            }
            Container.Register<IItemConsumptionService, ItemConsumptionService>();
            if (_farmingManager != null) Container.RegisterInstance(_farmingManager);
            if (_cropSelectionSystem != null) Container.RegisterInstance<ICropSelectionSystem>(_cropSelectionSystem);
            if (_fishingSystem != null) Container.RegisterInstance(_fishingSystem);
            if (_grid != null) Container.RegisterInstance(_grid);
            
            if (_buildingManager != null) Container.RegisterInstance(_buildingManager);
            if (_buildingSystem != null) Container.RegisterInstance(_buildingSystem);
            if (_buildingDatabase != null) Container.RegisterInstance(_buildingDatabase);
            
            if (_questSystem != null) Container.RegisterInstance(_questSystem);
            if (_craftingSystem != null) Container.RegisterInstance(_craftingSystem);
            if (_cookingSystem != null) Container.RegisterInstance(_cookingSystem);

            
            if (_weatherSystem != null) Container.RegisterInstance(_weatherSystem);
            
            if (_tutorialManager != null) Container.RegisterInstance(_tutorialManager);
            if (_uiManager != null)
            {
                Container.RegisterInstance(_uiManager);
                Container.RegisterInstance<IUIManager>(_uiManager);
            }
            if (_itemSpriteManager != null) Container.RegisterInstance<IItemSpriteManager>(_itemSpriteManager);
            if (_localizationManager != null)
            {
                Container.RegisterInstance(_localizationManager);
                Container.RegisterInstance<ILocalizationManager>(_localizationManager);
            }
            
            if (_gameData != null) Container.RegisterInstance(_gameData);
        }
    }
}
