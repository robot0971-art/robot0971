using System;
using System.Collections.Generic;
using UnityEngine;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.Localization
{
    /// <summary>
    /// 현지화 매니저 인터페이스
    /// </summary>
    public interface ILocalizationManager
    {
        Language CurrentLanguage { get; }
        void SetLanguage(Language language);
        string GetText(string key);
        string GetTextFormatted(string key, params object[] args);
        bool HasKey(string key);
        void RegisterData(LocalizationData data);
        void UnregisterData(LocalizationData data);
    }

    /// <summary>
    /// 언어 설정 저장 데이터
    /// </summary>
    [Serializable]
    public class LanguageSaveData
    {
        public int languageIndex;
    }

    /// <summary>
    /// 현지화 시스템 관리 매니저
    /// </summary>
    public class LocalizationManager : MonoBehaviour, ILocalizationManager, ISaveable
    {
        public static LocalizationManager Instance { get; private set; }

        private const string LANGUAGE_PREF_KEY = "Localization_Language";

        [Header("=== Default Data ===")]
        [SerializeField] private List<LocalizationData> _defaultData = new List<LocalizationData>();

        private Language _currentLanguage = Language.Korean;
        private readonly Dictionary<Language, List<LocalizationData>> _languageData = new Dictionary<Language, List<LocalizationData>>();
        private readonly Dictionary<Language, Dictionary<string, string>> _cachedStrings = new Dictionary<Language, Dictionary<string, string>>();

        public string SaveKey => "LocalizationManager";
        public Language CurrentLanguage => _currentLanguage;

        public event Action<Language> OnLanguageChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChangedEvent);
        }

        /// <summary>
        /// 초기화
        /// </summary>
        private void Initialize()
        {
            // 언어별 데이터 리스트 초기화
            foreach (Language lang in Enum.GetValues(typeof(Language)))
            {
                _languageData[lang] = new List<LocalizationData>();
                _cachedStrings[lang] = new Dictionary<string, string>();
            }

            // 기본 데이터 등록
            foreach (var data in _defaultData)
            {
                if (data != null)
                {
                    RegisterData(data);
                }
            }

            // 저장된 언어 설정 불러오기
            LoadLanguagePreference();

            // 이벤트 구독
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChangedEvent);

            Debug.Log($"[LocalizationManager] Initialized with language: {_currentLanguage}");
        }

        /// <summary>
        /// 언어 설정 불러오기
        /// </summary>
        private void LoadLanguagePreference()
        {
            if (PlayerPrefs.HasKey(LANGUAGE_PREF_KEY))
            {
                int savedIndex = PlayerPrefs.GetInt(LANGUAGE_PREF_KEY);
                if (Enum.IsDefined(typeof(Language), savedIndex))
                {
                    _currentLanguage = (Language)savedIndex;
                }
            }
            else
            {
                // 시스템 언어로 초기 설정
                _currentLanguage = GetLanguageFromSystemLanguage();
            }
        }

        /// <summary>
        /// 시스템 언어에서 게임 언어 변환
        /// </summary>
        private Language GetLanguageFromSystemLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Korean:
                    return Language.Korean;
                case SystemLanguage.Japanese:
                    return Language.Japanese;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return Language.Chinese;
                default:
                    return Language.English;
            }
        }

        /// <summary>
        /// 언어 설정 저장
        /// </summary>
        private void SaveLanguagePreference()
        {
            PlayerPrefs.SetInt(LANGUAGE_PREF_KEY, (int)_currentLanguage);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 언어 변경
        /// </summary>
        /// <param name="language">변경할 언어</param>
        public void SetLanguage(Language language)
        {
            if (_currentLanguage == language) return;

            var previousLanguage = _currentLanguage;
            _currentLanguage = language;
            SaveLanguagePreference();

            // 언어 변경 이벤트 발행
            EventBus.Publish(new LanguageChangedEvent
            {
                PreviousLanguage = previousLanguage,
                NewLanguage = _currentLanguage
            });

            OnLanguageChanged?.Invoke(_currentLanguage);

            Debug.Log($"[LocalizationManager] Language changed: {previousLanguage} -> {_currentLanguage}");
        }

        /// <summary>
        /// 현지화된 문자열 조회
        /// </summary>
        /// <param name="key">문자열 키</param>
        /// <returns>현지화된 문자열 (없으면 키 반환)</returns>
        public string GetText(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            // 캐시된 문자열 조회
            var cache = _cachedStrings[_currentLanguage];
            if (cache.TryGetValue(key, out var value))
            {
                return value;
            }

            // 모든 등록된 데이터에서 검색
            var dataList = _languageData[_currentLanguage];
            foreach (var data in dataList)
            {
                if (data.HasKey(key))
                {
                    var text = data.GetText(key);
                    cache[key] = text;
                    return text;
                }
            }

            // 다른 언어에서 폴백 검색
            string fallbackText = FindInOtherLanguages(key);
            if (fallbackText != null)
            {
                cache[key] = fallbackText;
                return fallbackText;
            }

            Debug.LogWarning($"[LocalizationManager] Key not found: {key}");
            return key;
        }

        /// <summary>
        /// 다른 언어에서 키 검색 (폴백)
        /// </summary>
        private string FindInOtherLanguages(string key)
        {
            foreach (Language lang in Enum.GetValues(typeof(Language)))
            {
                if (lang == _currentLanguage) continue;

                var dataList = _languageData[lang];
                foreach (var data in dataList)
                {
                    if (data.HasKey(key))
                    {
                        return data.GetText(key);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 포맷팅된 현지화 문자열 조회
        /// </summary>
        /// <param name="key">문자열 키</param>
        /// <param name="args">포맷팅 인자</param>
        /// <returns>포맷팅된 문자열</returns>
        public string GetTextFormatted(string key, params object[] args)
        {
            string text = GetText(key);

            try
            {
                return string.Format(text, args);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[LocalizationManager] Format error for key '{key}': {e.Message}");
                return text;
            }
        }

        /// <summary>
        /// 키 존재 여부 확인
        /// </summary>
        public bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            var dataList = _languageData[_currentLanguage];
            foreach (var data in dataList)
            {
                if (data.HasKey(key)) return true;
            }

            return false;
        }

        /// <summary>
        /// 현지화 데이터 등록
        /// </summary>
        /// <param name="data">등록할 데이터</param>
        public void RegisterData(LocalizationData data)
        {
            if (data == null) return;

            var lang = data.Language;
            if (!_languageData[lang].Contains(data))
            {
                data.Initialize();
                _languageData[lang].Add(data);

                // 캐시 무효화
                _cachedStrings[lang].Clear();

                Debug.Log($"[LocalizationManager] Registered data for language: {lang}");
            }
        }

        /// <summary>
        /// 현지화 데이터 등록 해제
        /// </summary>
        /// <param name="data">해제할 데이터</param>
        public void UnregisterData(LocalizationData data)
        {
            if (data == null) return;

            var lang = data.Language;
            if (_languageData[lang].Remove(data))
            {
                // 캐시 무효화
                _cachedStrings[lang].Clear();

                Debug.Log($"[LocalizationManager] Unregistered data for language: {lang}");
            }
        }

        /// <summary>
        /// 언어 변경 이벤트 핸들러 (외부에서 발생한 이벤트 처리)
        /// </summary>
        private void OnLanguageChangedEvent(LanguageChangedEvent evt)
        {
            // 내부에서 발생한 이벤트는 이미 처리됨
            // 외부 시스템에서 발생한 이벤트 처리가 필요한 경우 여기에 추가
        }

        #region ISaveable

        public object GetSaveData()
        {
            return new LanguageSaveData
            {
                languageIndex = (int)_currentLanguage
            };
        }

        public void LoadSaveData(object data)
        {
            if (data is LanguageSaveData saveData)
            {
                if (Enum.IsDefined(typeof(Language), saveData.languageIndex))
                {
                    _currentLanguage = (Language)saveData.languageIndex;
                    SaveLanguagePreference();
                    Debug.Log($"[LocalizationManager] Loaded language: {_currentLanguage}");
                }
            }
        }

        #endregion
    }
}