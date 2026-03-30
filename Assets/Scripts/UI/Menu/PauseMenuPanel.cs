using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.UI.Menu
{
    public class PauseMenuPanel : UIPanel
    {
        [Header("=== Buttons ===")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _mainMenuButton;
        
        [Header("=== Sub Panels ===")]
        [SerializeField] private SettingsPanel _settingsPanel;
        [SerializeField] private GameObject _loadGamePanel;
        
        [Header("=== Info ===")]
        [SerializeField] private TextMeshProUGUI _playTimeText;
        
        [Inject]
        private GameManager _gameManager;
        [Inject]
        private SaveSystem _saveSystem;
        
        protected override void Awake()
        {
            base.Awake();
            _pauseGameWhenOpen = true;
            _isModal = true;
        }
        
        private void OnEnable()
        {
            _resumeButton?.onClick.AddListener(OnResumeClicked);
            _saveButton?.onClick.AddListener(OnSaveClicked);
            _loadButton?.onClick.AddListener(OnLoadClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        }
        
        private void OnDisable()
        {
            _resumeButton?.onClick.RemoveListener(OnResumeClicked);
            _saveButton?.onClick.RemoveListener(OnSaveClicked);
            _loadButton?.onClick.RemoveListener(OnLoadClicked);
            _settingsButton?.onClick.RemoveListener(OnSettingsClicked);
            _mainMenuButton?.onClick.RemoveListener(OnMainMenuClicked);
        }
        
        protected override void OnOpened()
        {
            base.OnOpened();
            UpdatePlayTimeDisplay();
            
            if (_gameManager != null)
            {
                _gameManager.PauseGame();
            }
        }
        
        protected override void OnClosed()
        {
            base.OnClosed();
            
            if (_gameManager != null)
            {
                _gameManager.ResumeGame();
            }
        }
        
        private void UpdatePlayTimeDisplay()
        {
            if (_playTimeText != null && _saveSystem != null)
            {
                _playTimeText.text = $"플레이 시간: {_saveSystem.GetFormattedPlayTime()}";
            }
        }
        
        private void OnResumeClicked()
        {
            Close();
        }
        
        private void OnSaveClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.SaveGame();
            }
        }
        
        private void OnLoadClicked()
        {
            if (_loadGamePanel != null)
            {
                _loadGamePanel.SetActive(true);
            }
        }
        
        private void OnSettingsClicked()
        {
            if (_settingsPanel != null)
            {
                UIManager.Instance.OpenPanel(_settingsPanel);
            }
        }
        
        private void OnMainMenuClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.ReturnToMainMenu();
            }
        }
        
        public void LoadSave(string saveName)
        {
            if (_gameManager != null)
            {
                _gameManager.LoadGame(saveName);
                Close();
            }
            
            if (_loadGamePanel != null)
            {
                _loadGamePanel.SetActive(false);
            }
        }
        
        public void CloseLoadPanel()
        {
            if (_loadGamePanel != null)
            {
                _loadGamePanel.SetActive(false);
            }
        }
    }
}