using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.UI;

namespace SunnysideIsland.UI.Menu
{
    public class MainMenuPanel : UIPanel
    {
        [Header("=== Buttons ===")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _loadGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("=== Scenes ===")]
        [SerializeField] private string _gameSceneName = "MainGame";

        [Header("=== Sub Panels ===")]
        [SerializeField] private SaveLoadPanel _loadGamePanel;
        [SerializeField] private SettingsPanel _settingsPanel;

        [Header("=== Version ===")]
        [SerializeField] private TextMeshProUGUI _versionText;

        [Inject(Optional = true)]
        private GameManager _gameManager;

        private SaveSystem _saveSystem;

        private SaveLoadPanel _runtimeLoadGamePanel;

        protected override void Awake()
        {
            base.Awake();

            ResolveGameManager();
            _newGameButton?.onClick.AddListener(OnNewGameClicked);
            _loadGameButton?.onClick.AddListener(OnLoadGameClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _quitButton?.onClick.AddListener(OnQuitClicked);
        }

        private void Start()
        {
            EnsureMenuCoreSystems();
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
            GameManager.PrepareNewGameLaunch();
            if (!string.IsNullOrWhiteSpace(_gameSceneName))
            {
                Close();
                SceneManager.LoadScene(_gameSceneName);
            }
        }

        private void OnLoadGameClicked()
        {
            EnsureMenuCoreSystems();

            var panel = ResolveLoadGamePanel();
            if (panel != null)
            {
                panel.SetMode(false);
                panel.Open();
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
            ResolveGameManager();
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
            ResolveGameManager();
            if (_gameManager != null && !string.Equals(SceneManager.GetActiveScene().name, "Start Scene", StringComparison.OrdinalIgnoreCase))
            {
                _gameManager.LoadGame(saveName);
                Close();
                return;
            }

            Close();
            GameManager.PrepareLoadGame(saveName);
            SceneManager.LoadScene(_gameSceneName);
        }

        public void CloseLoadPanel()
        {
            var panel = ResolveLoadGamePanel();
            if (panel != null)
            {
                panel.Close();
            }
        }

        private SaveLoadPanel ResolveLoadGamePanel()
        {
            if (_loadGamePanel != null)
            {
                if (_loadGamePanel.gameObject.scene.IsValid() && _loadGamePanel.gameObject.scene.isLoaded)
                {
                    return _loadGamePanel;
                }

                if (_runtimeLoadGamePanel == null)
                {
                    var parent = transform.parent;
                    _runtimeLoadGamePanel = Instantiate(_loadGamePanel, parent);
                    _runtimeLoadGamePanel.name = _loadGamePanel.name;
                }

                return _runtimeLoadGamePanel;
            }

            return _runtimeLoadGamePanel;
        }

        private void EnsureMenuCoreSystems()
        {
            if (_saveSystem == null)
            {
                _saveSystem = FindFirstObjectByType<SaveSystem>(FindObjectsInactive.Include);
            }

            if (_saveSystem == null)
            {
                var saveSystemGO = new GameObject("[MenuSaveSystem]");
                _saveSystem = saveSystemGO.AddComponent<SaveSystem>();
            }
        }

        private void ResolveGameManager()
        {
            if (_gameManager != null)
            {
                return;
            }

            _gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        }
    }
}
