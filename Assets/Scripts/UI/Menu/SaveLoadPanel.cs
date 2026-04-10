using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using SunnysideIsland.Core;
using DI;
using TMPro;

namespace SunnysideIsland.UI.Menu
{
    /// <summary>
    /// 저장/불러오기 전체 화면을 관리하는 패널
    /// </summary>
    public class SaveLoadPanel : UIPanel
    {
        [Header("=== UI References ===")]
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _closeButton;
        
        [Header("=== Settings ===")]
        [SerializeField] private bool _isSaveMode = false;

        private SaveSystem _saveSystem;
        private GameManager _gameManager;

        private bool _isRefreshing = false;

        protected override void Awake()
        {
            base.Awake();

            RefreshDependencies();
        }

        private void OnEnable()
        {
            if (_saveButton != null)
            {
                _saveButton.onClick.AddListener(CreateNewSave);
            }
            
            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
            }
            
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(CloseViaUIManager);
            }
        }

        private void OnDisable()
        {
            if (_saveButton != null)
            {
                _saveButton.onClick.RemoveListener(CreateNewSave);
            }
            
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(OnQuitClicked);
            }
            
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(CloseViaUIManager);
            }
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            RefreshDependencies();
            Debug.Log($"[SaveLoadPanel] OnOpened called | Frame: {Time.frameCount}");
            RefreshList();
        }

        protected override void OnClosed()
        {
            base.OnClosed();
        }

        /// <summary>
        /// 저장 목록 갱신
        /// </summary>
        public void RefreshList()
        {
            if (_isRefreshing)
            {
                Debug.LogWarning($"[SaveLoadPanel] RefreshList already in progress, skipping | Frame: {Time.frameCount}");
                return;
            }

            _isRefreshing = true;

            try
            {
                Debug.Log($"[SaveLoadPanel] RefreshList Called | Frame: {Time.frameCount} | Mode: {(_isSaveMode ? "Save" : "Load")}");

                if (_slotContainer == null)
                {
                    return;
                }

                int destroyedCount = 0;
                List<GameObject> childrenToDestroy = new List<GameObject>();
                foreach (Transform child in _slotContainer)
                {
                    childrenToDestroy.Add(child.gameObject);
                }

                foreach (var child in childrenToDestroy)
                {
                    if (child != null)
                    {
                        Destroy(child);
                        destroyedCount++;
                    }
                }

                Debug.Log($"[SaveLoadPanel] Destroyed {destroyedCount} existing slots");

                if (_saveSystem == null)
                {
                    return;
                }

                var saves = _saveSystem.GetSaveList();

                Debug.Log($"[SaveLoadPanel] Found {saves.Count} save files");

                if (_titleText != null)
                {
                    _titleText.text = _isSaveMode ? "Save Game" : "Load Game";
                }

                int createdCount = 0;
                foreach (var metadata in saves)
                {
                    if (_slotPrefab != null)
                    {
                        var go = Instantiate(_slotPrefab, _slotContainer);
                        var slot = go.GetComponent<SaveSlotUI>();
                        if (slot != null)
                        {
                            slot.Setup(metadata, this);
                            createdCount++;
                        }
                    }
                }

                Debug.Log($"[SaveLoadPanel] Created {createdCount} new slots");
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// 슬롯 클릭 시 호출 (불러오기 또는 덮어쓰기)
        /// </summary>
        public void OnSlotSelected(string saveName)
        {
            RefreshDependencies();
            Debug.Log($"[SaveLoadPanel] Slot Selected: {saveName} | Mode: {(_isSaveMode ? "Save" : "Load")} | GM: {(_gameManager != null)}");
            if (_isSaveMode)
            {
                _gameManager?.SaveGame(saveName);
                RefreshList();
            }
            else
            {
                Debug.Log($"[SaveLoadPanel] Calling GameManager.LoadGame({saveName})");
                var activeSceneName = SceneManager.GetActiveScene().name;
                if (string.Equals(activeSceneName, "Start Scene", StringComparison.OrdinalIgnoreCase))
                {
                    CloseViaUIManager();
                    GameManager.PrepareLoadGame(saveName);
                    SceneManager.LoadScene(GameManager.DefaultGameSceneName);
                }
                else if (_gameManager != null)
                {
                    _gameManager.LoadGame(saveName);
                    CloseViaUIManager();
                }
                else
                {
                    CloseViaUIManager();
                    GameManager.PrepareLoadGame(saveName);
                    SceneManager.LoadScene(GameManager.DefaultGameSceneName);
                }
            }
        }

        /// <summary>
        /// 삭제 버튼 클릭 시 호출
        /// </summary>
        public void OnDeleteSelected(string saveName)
        {
            RefreshDependencies();
            _saveSystem?.DeleteSave(saveName);
            RefreshList();
        }

        /// <summary>
        /// 새 저장 파일 생성 (Save Mode 전용)
        /// </summary>
        public void CreateNewSave()
        {
            RefreshDependencies();
            string newSaveName = $"Save_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            _gameManager?.SaveGame(newSaveName);
            RefreshList();
        }

        public void SetMode(bool isSaveMode)
        {
            _isSaveMode = isSaveMode;
            RefreshList();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void CloseViaUIManager()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePanel(this);
            }
            else
            {
                Close();
            }
        }

        private void RefreshDependencies()
        {
            _saveSystem = FindFirstObjectByType<SaveSystem>(FindObjectsInactive.Include);
            _gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        }
    }
}
