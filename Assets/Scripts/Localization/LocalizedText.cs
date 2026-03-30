using System;
using UnityEngine;
using TMPro;
using DI;
using SunnysideIsland.Events;

namespace SunnysideIsland.Localization
{
    /// <summary>
    /// UI 텍스트 자동 현지화 컴포넌트
    /// TextMeshProUGUI와 함께 사용하여 언어 변경 시 자동으로 텍스트 업데이트
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("=== Localization Settings ===")]
        [SerializeField] private string _localizationKey;
        [SerializeField] private bool _updateOnEnable = true;
        [SerializeField] private bool _useFormatting;
        [SerializeField] private object[] _formatArgs;

        private TextMeshProUGUI _textComponent;
        private LocalizationManager _localizationManager;
        private bool _isInitialized;

        /// <summary>
        /// 현지화 키
        /// </summary>
        public string LocalizationKey
        {
            get => _localizationKey;
            set
            {
                _localizationKey = value;
                if (_isInitialized)
                {
                    UpdateText();
                }
            }
        }

        /// <summary>
        /// 포맷팅 인자
        /// </summary>
        public object[] FormatArgs
        {
            get => _formatArgs;
            set
            {
                _formatArgs = value;
                if (_isInitialized && _useFormatting)
                {
                    UpdateText();
                }
            }
        }

        private void Awake()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            if (_updateOnEnable)
            {
                UpdateText();
            }

            // 언어 변경 이벤트 구독
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
        }

        private void OnDisable()
        {
            // 언어 변경 이벤트 구독 해제
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
        }

        /// <summary>
        /// 초기화
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized) return;

            // DI를 통해 LocalizationManager 주입 시도
            if (DIContainer.TryResolve(out ILocalizationManager manager))
            {
                _localizationManager = manager as LocalizationManager;
            }

            // 주입 실패 시 싱글톤 인스턴스 사용
            if (_localizationManager == null)
            {
                _localizationManager = LocalizationManager.Instance;
            }

            if (_localizationManager == null)
            {
                Debug.LogWarning($"[LocalizedText] LocalizationManager not found on {gameObject.name}");
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 텍스트 업데이트
        /// </summary>
        public void UpdateText()
        {
            if (string.IsNullOrEmpty(_localizationKey))
            {
                return;
            }

            if (_textComponent == null)
            {
                _textComponent = GetComponent<TextMeshProUGUI>();
                if (_textComponent == null)
                {
                    Debug.LogError($"[LocalizedText] TextMeshProUGUI not found on {gameObject.name}");
                    return;
                }
            }

            if (_localizationManager == null)
            {
                // 다시 시도
                if (DIContainer.TryResolve(out ILocalizationManager manager))
                {
                    _localizationManager = manager as LocalizationManager;
                }
                else
                {
                    _localizationManager = LocalizationManager.Instance;
                }

                if (_localizationManager == null)
                {
                    _textComponent.text = _localizationKey;
                    return;
                }
            }

            string text;
            if (_useFormatting && _formatArgs != null && _formatArgs.Length > 0)
            {
                text = _localizationManager.GetTextFormatted(_localizationKey, _formatArgs);
            }
            else
            {
                text = _localizationManager.GetText(_localizationKey);
            }

            _textComponent.text = text;
        }

        /// <summary>
        /// 포맷팅 인자 설정 및 텍스트 업데이트
        /// </summary>
        /// <param name="args">포맷팅 인자</param>
        public void SetFormatArgs(params object[] args)
        {
            _formatArgs = args;
            _useFormatting = true;

            if (_isInitialized)
            {
                UpdateText();
            }
        }

        /// <summary>
        /// 키와 포맷팅 인자 설정
        /// </summary>
        /// <param name="key">현지화 키</param>
        /// <param name="args">포맷팅 인자</param>
        public void SetKeyAndArgs(string key, params object[] args)
        {
            _localizationKey = key;
            _formatArgs = args;
            _useFormatting = args != null && args.Length > 0;

            if (_isInitialized)
            {
                UpdateText();
            }
        }

        /// <summary>
        /// 언어 변경 이벤트 핸들러
        /// </summary>
        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            UpdateText();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 키 변경 시 자동 업데이트
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying && _isInitialized && !string.IsNullOrEmpty(_localizationKey))
            {
                UpdateText();
            }
        }
#endif
    }
}