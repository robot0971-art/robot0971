using System;
using DI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.UI.Menu
{
    public sealed class GameOverPanel : UIPanel
    {
        [Header("=== UI ===")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _quitButton;

        [Header("=== Settings ===")]
        [SerializeField] private string _defaultMessage = "Game Over";
        [SerializeField] private string _loadSaveName = "autosave";
        [SerializeField] private string _fallbackSaveName = "death_autosave";

        [Inject(Optional = true)]
        private SaveSystem _saveSystem;

        protected override void Awake()
        {
            base.Awake();

            _isModal = true;
            _pauseGameWhenOpen = true;
            _closeOnEscape = false;

            EventBus.Subscribe<GameOverEvent>(OnGameOverEvent);
            if (_saveSystem == null)
            {
                DIContainer.TryResolve(out _saveSystem);
            }
            ResolveReferences();
            HookButtons();
            ApplyDefaultMessage();
            SetHiddenState();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOverEvent);
        }

        private void ResolveReferences()
        {
            if (_messageText == null)
            {
                _messageText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (_loadButton == null || _quitButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                foreach (var button in buttons)
                {
                    if (button == null)
                    {
                        continue;
                    }

                    string lowerName = button.name.ToLowerInvariant();
                    if (_loadButton == null && lowerName.Contains("load"))
                    {
                        _loadButton = button;
                    }
                    else if (_quitButton == null && (lowerName.Contains("quit") || lowerName.Contains("exit")))
                    {
                        _quitButton = button;
                    }
                }
            }
        }

        private void HookButtons()
        {
            if (_loadButton != null)
            {
                _loadButton.onClick.RemoveListener(OnLoadClicked);
                _loadButton.onClick.AddListener(OnLoadClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(OnQuitClicked);
                _quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void ApplyDefaultMessage()
        {
            if (_messageText != null && string.IsNullOrWhiteSpace(_messageText.text))
            {
                _messageText.text = _defaultMessage;
            }
        }

        private void SetHiddenState()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_messageText != null)
            {
                _messageText.gameObject.SetActive(false);
            }

            if (_loadButton != null)
            {
                _loadButton.gameObject.SetActive(false);
            }

            if (_quitButton != null)
            {
                _quitButton.gameObject.SetActive(false);
            }

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnGameOverEvent(GameOverEvent evt)
        {
            if (IsOpen)
            {
                return;
            }

            if (_messageText != null && !string.IsNullOrWhiteSpace(_defaultMessage))
            {
                _messageText.text = _defaultMessage;
            }

            if (_messageText != null)
            {
                _messageText.gameObject.SetActive(true);
            }

            if (_loadButton != null)
            {
                _loadButton.gameObject.SetActive(true);
            }

            if (_quitButton != null)
            {
                _quitButton.gameObject.SetActive(true);
            }

            Open();
        }

        private void OnLoadClicked()
        {
            Close();

            if (GameManager.Instance != null)
            {
                string saveName = ResolveRetrySaveName();
                if (!string.IsNullOrWhiteSpace(saveName))
                {
                    GameManager.Instance.LoadGame(saveName);
                }
            }
        }

        private string ResolveRetrySaveName()
        {
            if (GameManager.Instance != null)
            {
                string lastPlayableSave = GameManager.Instance.LastPlayableSaveName;
                if (!string.IsNullOrWhiteSpace(lastPlayableSave) &&
                    _saveSystem != null &&
                    _saveSystem.SaveExists(lastPlayableSave))
                {
                    return lastPlayableSave;
                }
            }

            string latestPlayableSave = ResolveLatestPlayableSaveName();
            if (!string.IsNullOrWhiteSpace(latestPlayableSave))
            {
                return latestPlayableSave;
            }

            if (_saveSystem != null && !string.IsNullOrWhiteSpace(_loadSaveName) && _saveSystem.SaveExists(_loadSaveName))
            {
                return _loadSaveName;
            }

            if (_saveSystem != null && !string.IsNullOrWhiteSpace(_fallbackSaveName) && _saveSystem.SaveExists(_fallbackSaveName))
            {
                return _fallbackSaveName;
            }

            return _loadSaveName;
        }

        private string ResolveLatestPlayableSaveName()
        {
            if (_saveSystem == null)
            {
                return null;
            }

            var saves = _saveSystem.GetSaveList();
            if (saves == null || saves.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < saves.Count; i++)
            {
                string saveName = saves[i]?.SaveName;
                if (string.IsNullOrWhiteSpace(saveName))
                {
                    continue;
                }

                if (string.Equals(saveName, "autosave", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(saveName, "death_autosave", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return saveName;
            }

            return null;
        }

        private void OnQuitClicked()
        {
            Close();
            GameManager.Instance?.QuitGame();
        }
    }
}
