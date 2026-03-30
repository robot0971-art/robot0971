using UnityEngine;
using DI;
using SunnysideIsland.UI;
using SunnysideIsland.Localization;

namespace DI
{
    public class MainMenuSceneInstaller : SceneInstaller
    {
        [Header("=== UI Systems ===")]
        [SerializeField] private UIManager _uiManager;
        
        [Header("=== Data ===")]
        [SerializeField] private LocalizationManager _localizationManager;
        
        protected override void InstallSceneBindings()
        {
            if (_uiManager != null) Container.RegisterInstance<IUIManager>(_uiManager);
            if (_localizationManager != null) Container.RegisterInstance<ILocalizationManager>(_localizationManager);
        }
    }
}