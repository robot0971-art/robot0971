using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.UI.Menu
{
    public class MainMenuPanel : UIPanel
    {
        [Header("=== Buttons ===")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _loadGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("=== Sub Panels ===")]
        [SerializeField] private GameObject _loadGamePanel;
        [SerializeField] private SettingsPanel _settingsPanel;

        [Header("=== Version ===")]
        [SerializeField] private TextMeshProUGUI _versionText;

        [Inject]
        private GameManager _gameManager;
        [Inject]
        private SaveSystem _saveSystem;

        public void Awake()
        {
            _newGameButton?.onClick.AddListener(OnNewGameClicked);
            _loadGameButton?.onClick.AddListener(OnLoadGameClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _quitButton?.onClick.AddListener(OnQuitClicked);
        }

        private void Start()
        {
            UpdateVersionText();
            UpdateLoadButtonState();
        }

        private void UpdateVersionText()
        {
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }
        }

        private void UpdateLoadButtonState()
        {
            if (_loadGameButton != null && _saveSystem != null)
            {
                var saves = _saveSystem.GetSaveList();
                _loadGameButton.interactable = saves.Count > 0;
            }
        }

        private void OnNewGameClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.StartNewGame();
                Close();
            }
        }

        private void OnLoadGameClicked()
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

        private void OnQuitClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.QuitGame();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
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