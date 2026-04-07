using UnityEngine;
using UnityEngine.UI;
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
        
        [Header("=== Settings ===")]
        [SerializeField] private bool _isSaveMode = false;

        [Inject] private SaveSystem _saveSystem;
        [Inject] private GameManager _gameManager;

        private bool _isRefreshing = false;

        private void Awake()
        {
            if (_saveSystem == null)
                _saveSystem = DIContainer.Resolve<SaveSystem>();
            if (_gameManager == null)
                _gameManager = DIContainer.Resolve<GameManager>();
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
        }

        protected override void OnOpened()
        {
            base.OnOpened();
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
            
            Debug.Log($"[SaveLoadPanel] RefreshList Called | Frame: {Time.frameCount} | Mode: {(_isSaveMode ? "Save" : "Load")}");
            Debug.Log($"[SaveLoadPanel] Call Stack:\n{System.Environment.StackTrace}");
            
            if (_slotContainer == null)
            {
                _isRefreshing = false;
                return;
            }

            // 기존 슬롯 즉시 제거
            int destroyedCount = 0;
            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in _slotContainer)
            {
                childrenToDestroy.Add(child.gameObject);
            }
            
            foreach (var child in childrenToDestroy)
            {
                DestroyImmediate(child);
                destroyedCount++;
            }
            
            Debug.Log($"[SaveLoadPanel] Destroyed {destroyedCount} existing slots");

            if (_saveSystem == null)
            {
                _isRefreshing = false;
                return;
            }

            // 저장 데이터 메타데이터 가져오기
            var saves = _saveSystem.GetSaveList();
            
            Debug.Log($"[SaveLoadPanel] Found {saves.Count} save files");
            
            if (_titleText != null)
            {
                _titleText.text = _isSaveMode ? "Save Game" : "Load Game";
            }

            // 슬롯 생성 및 데이터 설정
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
            _isRefreshing = false;
        }

        /// <summary>
        /// 슬롯 클릭 시 호출 (불러오기 또는 덮어쓰기)
        /// </summary>
        public void OnSlotSelected(string saveName)
        {
            if (_isSaveMode)
            {
                _gameManager?.SaveGame(saveName);
                RefreshList();
            }
            else
            {
                _gameManager?.LoadGame(saveName);
                Close();
            }
        }

        /// <summary>
        /// 삭제 버튼 클릭 시 호출
        /// </summary>
        public void OnDeleteSelected(string saveName)
        {
            _saveSystem?.DeleteSave(saveName);
            RefreshList();
        }

        /// <summary>
        /// 새 저장 파일 생성 (Save Mode 전용)
        /// </summary>
        public void CreateNewSave()
        {
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
    }
}
