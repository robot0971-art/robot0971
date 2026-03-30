using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SunnysideIsland.Events;

namespace SunnysideIsland.Core
{
    /// <summary>
    /// 저장 데이터 인터페이스
    /// </summary>
    public interface ISaveable
    {
        string SaveKey { get; }
        object GetSaveData();
        void LoadSaveData(object data);
    }

    /// <summary>
    /// 게임 저장 시스템
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        [Header("=== Settings ===")]
        [SerializeField] private bool _autoSave = true;
        [SerializeField] private float _autoSaveInterval = 300f; // 5분
        
        // 저장 경로
        private string SaveDirectory => Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "SunnysideIsland",
            "Saves"
        );
        
        private string GetSavePath(string saveName)
        {
            return Path.Combine(SaveDirectory, $"{saveName}.json");
        }
        
        // 저장 가능한 오브젝트들
        private readonly List<ISaveable> _saveables = new();
        private float _autoSaveTimer;
        private float _totalPlayTime;
        private float _sessionStartTime;
        private bool _isTrackingTime;
        
        private void Awake()
        {
            EnsureDirectoryExists();
        }
        
        private void Update()
        {
            if (_autoSave)
            {
                _autoSaveTimer += Time.unscaledDeltaTime;
                if (_autoSaveTimer >= _autoSaveInterval)
                {
                    AutoSave();
                    _autoSaveTimer = 0f;
                }
            }
            
            if (_isTrackingTime)
            {
                _totalPlayTime += Time.unscaledDeltaTime;
            }
        }
        
        /// <summary>
        /// 저장 디렉토리 확인/생성
        /// </summary>
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
                Debug.Log($"[SaveSystem] Created save directory: {SaveDirectory}");
            }
        }
        
        /// <summary>
        /// 저장 가능한 오브젝트 등록
        /// </summary>
        public void Register(ISaveable saveable)
        {
            if (!_saveables.Contains(saveable))
            {
                _saveables.Add(saveable);
            }
        }
        
        /// <summary>
        /// 저장 가능한 오브젝트 등록 해제
        /// </summary>
        public void Unregister(ISaveable saveable)
        {
            _saveables.Remove(saveable);
        }
        
        /// <summary>
        /// 게임 저장
        /// </summary>
        public void SaveGame(string saveName)
        {
            try
            {
                EnsureDirectoryExists();
                
                var saveData = new GameSaveData
                {
                    SaveId = saveName,
                    SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Version = Application.version,
                    PlayTime = GetTotalPlayTime()
                };
                
                // 각 저장 가능한 오브젝트의 데이터 수집
                foreach (var saveable in _saveables)
                {
                    try
                    {
                        var data = saveable.GetSaveData();
                        if (data != null)
                        {
                            saveData.Data[saveable.SaveKey] = data;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SaveSystem] Error saving {saveable.SaveKey}: {e.Message}");
                    }
                }
                
                // JSON으로 직렬화
                string json = JsonUtility.ToJson(saveData, true);
                string savePath = GetSavePath(saveName);
                File.WriteAllText(savePath, json);
                
                Debug.Log($"[SaveSystem] Game saved: {saveName}");
                
                EventBus.Publish(new GameSavedEvent
                {
                    SaveName = saveName,
                    Success = true
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save game: {e.Message}");
                
                EventBus.Publish(new GameSavedEvent
                {
                    SaveName = saveName,
                    Success = false
                });
            }
        }
        
        /// <summary>
        /// 게임 불러오기
        /// </summary>
        public bool LoadGame(string saveName)
        {
            try
            {
                string savePath = GetSavePath(saveName);
                
                if (!File.Exists(savePath))
                {
                    Debug.LogWarning($"[SaveSystem] Save file not found: {saveName}");
                    return false;
                }
                
                string json = File.ReadAllText(savePath);
                var saveData = JsonUtility.FromJson<GameSaveData>(json);
                
                if (saveData == null)
                {
                    Debug.LogError($"[SaveSystem] Failed to deserialize save data: {saveName}");
                    return false;
                }
                
                // 각 저장 가능한 오브젝트에 데이터 로드
                foreach (var saveable in _saveables)
                {
                    try
                    {
                        if (saveData.Data.TryGetValue(saveable.SaveKey, out var data))
                        {
                            saveable.LoadSaveData(data);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SaveSystem] Error loading {saveable.SaveKey}: {e.Message}");
                    }
                }
                
                // 플레이 시간 복원
                _totalPlayTime = saveData.PlayTime;
                StartPlayTimeTracking();
                
                Debug.Log($"[SaveSystem] Game loaded: {saveName}");
                
                EventBus.Publish(new GameLoadedEvent
                {
                    SaveName = saveName,
                    Success = true
                });
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load game: {e.Message}");
                
                EventBus.Publish(new GameLoadedEvent
                {
                    SaveName = saveName,
                    Success = false
                });
                
                return false;
            }
        }
        
        /// <summary>
        /// 저장 파일 삭제
        /// </summary>
        public void DeleteSave(string saveName)
        {
            try
            {
                string savePath = GetSavePath(saveName);
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Debug.Log($"[SaveSystem] Save deleted: {saveName}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to delete save: {e.Message}");
            }
        }
        
        /// <summary>
        /// 저장 목록 조회
        /// </summary>
        public List<SaveMetadata> GetSaveList()
        {
            var saves = new List<SaveMetadata>();
            
            try
            {
                EnsureDirectoryExists();
                
                var files = Directory.GetFiles(SaveDirectory, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        var saveName = Path.GetFileNameWithoutExtension(file);
                        
                        // 저장 파일 메타데이터 읽기
                        string json = File.ReadAllText(file);
                        var saveData = JsonUtility.FromJson<GameSaveData>(json);
                        
                        if (saveData != null)
                        {
                            saves.Add(new SaveMetadata
                            {
                                SaveName = saveName,
                                SaveTime = saveData.SaveTime,
                                PlayTime = saveData.PlayTime,
                                Version = saveData.Version
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[SaveSystem] Error reading save file {file}: {e.Message}");
                    }
                }
                
                // 저장 시간순으로 정렬 (최신순)
                saves.Sort((a, b) => DateTime.Parse(b.SaveTime).CompareTo(DateTime.Parse(a.SaveTime)));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Error getting save list: {e.Message}");
            }
            
            return saves;
        }
        
        /// <summary>
        /// 저장 파일 존재 여부 확인
        /// </summary>
        public bool SaveExists(string saveName)
        {
            return File.Exists(GetSavePath(saveName));
        }
        
        /// <summary>
        /// 자동 저장
        /// </summary>
        private void AutoSave()
        {
            SaveGame("autosave");
        }
        
        /// <summary>
        /// 자동 저장 활성화/비활성화
        /// </summary>
        public void SetAutoSave(bool enabled)
        {
            _autoSave = enabled;
        }
        
        /// <summary>
        /// 총 플레이 시간 반환
        /// </summary>
        private float GetTotalPlayTime()
        {
            return _totalPlayTime;
        }
        
        /// <summary>
        /// 플레이 시간 추적 시작
        /// </summary>
        public void StartPlayTimeTracking()
        {
            _isTrackingTime = true;
            _sessionStartTime = Time.unscaledTime;
        }
        
        /// <summary>
        /// 플레이 시간 추적 중지
        /// </summary>
        public void StopPlayTimeTracking()
        {
            _isTrackingTime = false;
        }
        
        /// <summary>
        /// 플레이 시간 포맷팅 (HH:MM:SS)
        /// </summary>
        public string GetFormattedPlayTime()
        {
            int totalSeconds = Mathf.FloorToInt(_totalPlayTime);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }
        
        /// <summary>
        /// 저장 데이터 클래스
        /// </summary>
        [Serializable]
        public class GameSaveData
        {
            public string SaveId;
            public string SaveTime;
            public string Version;
            public float PlayTime;
            public Dictionary<string, object> Data = new();
        }
        
        /// <summary>
        /// 저장 메타데이터
        /// </summary>
        [Serializable]
        public class SaveMetadata
        {
            public string SaveName;
            public string SaveTime;
            public float PlayTime;
            public string Version;
        }
    }
}
