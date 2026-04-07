using UnityEngine;
using DI;
using SunnysideIsland.UI;
using SunnysideIsland.Localization;
using SunnysideIsland.Core;

namespace DI
{
    public class MainMenuSceneInstaller : SceneInstaller
    {
        [Header("=== Core Systems ===")]
        [SerializeField] private SaveSystem _saveSystem;
        [SerializeField] private GameManager _gameManager;

        [Header("=== UI Systems ===")]
        [SerializeField] private UIManager _uiManager;
        
        [Header("=== Data ===")]
        [SerializeField] private LocalizationManager _localizationManager;
        
        protected override void InstallSceneBindings()
        {
            if (_saveSystem != null) Container.RegisterInstance(_saveSystem);
            if (_gameManager != null) Container.RegisterInstance(_gameManager);

            if (_uiManager != null)
            {
                Container.RegisterInstance(_uiManager);
                Container.RegisterInstance<IUIManager>(_uiManager);
            }
            
            if (_localizationManager != null)
            {
                Container.RegisterInstance(_localizationManager);
                Container.RegisterInstance<ILocalizationManager>(_localizationManager);
            }
        }
    }
}
