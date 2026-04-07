using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SunnysideIsland.Core;
using System;

namespace SunnysideIsland.UI.Menu
{
    /// <summary>
    /// 저장 목록의 개별 슬롯을 관리하는 UI 스크립트
    /// </summary>
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("=== UI Elements ===")]
        [SerializeField] private TextMeshProUGUI _saveNameText;
        [SerializeField] private TextMeshProUGUI _playTimeText;
        [SerializeField] private TextMeshProUGUI _dateText;
        [SerializeField] private TextMeshProUGUI _versionText;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _deleteButton;

        private SaveSystem.SaveMetadata _metadata;
        private SaveLoadPanel _parentPanel;

        /// <summary>
        /// 슬롯 데이터 설정
        /// </summary>
        public void Setup(SaveSystem.SaveMetadata metadata, SaveLoadPanel parent)
        {
            _metadata = metadata;
            _parentPanel = parent;

            if (_saveNameText != null) _saveNameText.text = metadata.SaveName;
            if (_playTimeText != null) _playTimeText.text = FormatPlayTime(metadata.PlayTime);
            if (_dateText != null) _dateText.text = metadata.SaveTime;
            if (_versionText != null) _versionText.text = $"v{metadata.Version}";

            if (_loadButton != null)
            {
                _loadButton.onClick.RemoveAllListeners();
                _loadButton.onClick.AddListener(OnLoadClicked);
            }

            if (_deleteButton != null)
            {
                _deleteButton.onClick.RemoveAllListeners();
                _deleteButton.onClick.AddListener(OnDeleteClicked);
            }
        }

        private void OnLoadClicked()
        {
            _parentPanel?.OnSlotSelected(_metadata.SaveName);
        }

        private void OnDeleteClicked()
        {
            _parentPanel?.OnDeleteSelected(_metadata.SaveName);
        }

        private string FormatPlayTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;
            
            if (h > 0) return $"{h}h {m}m {s}s";
            if (m > 0) return $"{m}m {s}s";
            return $"{s}s";
        }
    }
}
