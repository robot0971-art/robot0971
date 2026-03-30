using System;
using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Localization
{
    /// <summary>
    /// 지원 언어 열거형
    /// </summary>
    public enum Language
    {
        Korean,
        English,
        Japanese,
        Chinese
    }

    /// <summary>
    /// 현지화 문자열 데이터를 저장하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationData", menuName = "SunnysideIsland/Localization Data")]
    public class LocalizationData : ScriptableObject
    {
        [Header("=== Language Settings ===")]
        [SerializeField] private Language _language = Language.Korean;

        [Header("=== String Data ===")]
        [SerializeField] private List<LocalizedString> _strings = new List<LocalizedString>();

        // 런타임 조회를 위한 딕셔너리
        private Dictionary<string, string> _stringDictionary;
        private bool _isInitialized;

        /// <summary>
        /// 현재 언어
        /// </summary>
        public Language Language => _language;

        /// <summary>
        /// 초기화 여부
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 딕셔너리 초기화
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _stringDictionary = new Dictionary<string, string>();

            foreach (var localizedString in _strings)
            {
                if (!string.IsNullOrEmpty(localizedString.Key))
                {
                    if (_stringDictionary.ContainsKey(localizedString.Key))
                    {
                        Debug.LogWarning($"[LocalizationData] Duplicate key found: {localizedString.Key}");
                        continue;
                    }
                    _stringDictionary[localizedString.Key] = localizedString.Value;
                }
            }

            _isInitialized = true;
            Debug.Log($"[LocalizationData] Initialized with {_stringDictionary.Count} strings for language: {_language}");
        }

        /// <summary>
        /// 키로 문자열 조회
        /// </summary>
        /// <param name="key">문자열 키</param>
        /// <returns>현지화된 문자열 (없으면 키 반환)</returns>
        public string GetText(string key)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            if (_stringDictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            Debug.LogWarning($"[LocalizationData] Key not found: {key}");
            return key;
        }

        /// <summary>
        /// 키 존재 여부 확인
        /// </summary>
        /// <param name="key">문자열 키</param>
        /// <returns>존재 여부</returns>
        public bool HasKey(string key)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _stringDictionary.ContainsKey(key);
        }

        /// <summary>
        /// 모든 키 반환
        /// </summary>
        /// <returns>키 컬렉션</returns>
        public IEnumerable<string> GetAllKeys()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _stringDictionary.Keys;
        }

        /// <summary>
        /// 문자열 개수 반환
        /// </summary>
        public int StringCount => _strings.Count;

#if UNITY_EDITOR
        /// <summary>
        /// 에디터용: 문자열 추가
        /// </summary>
        public void AddString(string key, string value)
        {
            _strings.Add(new LocalizedString { Key = key, Value = value });
            _isInitialized = false;
        }

        /// <summary>
        /// 에디터용: 문자열 제거
        /// </summary>
        public void RemoveString(string key)
        {
            _strings.RemoveAll(s => s.Key == key);
            _isInitialized = false;
        }

        /// <summary>
        /// 에디터용: 딕셔너리 클리어
        /// </summary>
        public void ClearCache()
        {
            _stringDictionary?.Clear();
            _isInitialized = false;
        }
#endif
    }

    /// <summary>
    /// 현지화 문자열 데이터 구조
    /// </summary>
    [Serializable]
    public class LocalizedString
    {
        [SerializeField] private string _key;
        [SerializeField] [TextArea(1, 5)] private string _value;

        public string Key { get => _key; set => _key = value; }
        public string Value { get => _value; set => _value = value; }
    }
}