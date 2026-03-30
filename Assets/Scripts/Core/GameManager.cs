using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using SunnysideIsland.Events;
using SunnysideIsland.Core;
using SunnysideIsland.Crafting;
using DI;

namespace SunnysideIsland.Core
{
    /// <summary>
    /// 게임의 전체 상태와 흐름을 관리하는 매니저
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("=== Settings ===")]
        [SerializeField] private string _gameSceneName = "Game";
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        
        [Header("=== References ===")]
        [SerializeField] private TimeManager _timeManager;
        [SerializeField] private SaveSystem _saveSystem;
        
        // 현재 게임 상태
        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public static GameManager Instance { get; private set; }
        
        // 현재 활성화된 세이브 이름
        public string CurrentSaveName { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// 초기화
        /// </summary>
        private void Initialize()
        {
            // DI Container 초기화
            DIContainer.InitializeGlobal();
            
            // 전역 서비스 등록
            RegisterGlobalServices();
            
            // 이벤트 구독
            EventBus.Subscribe<ItemCraftedEvent>(OnItemCrafted);
            
            Debug.Log("[GameManager] Initialized");
        }
        
        /// <summary>
        /// 전역 서비스 등록
        /// </summary>
        private void RegisterGlobalServices()
        {
            // TimeManager 등록
            if (_timeManager != null)
            {
                DIContainer.Global.RegisterInstance(_timeManager);
                _saveSystem.Register(_timeManager as ISaveable);
            }
            
            // SaveSystem 등록
            if (_saveSystem != null)
            {
                DIContainer.Global.RegisterInstance(_saveSystem);
            }
            
            // GameManager 등록
            DIContainer.Global.RegisterInstance(this);
        }
        
        /// <summary>
        /// 새 게임 시작
        /// </summary>
        public void StartNewGame()
        {
            CurrentSaveName = $"save_{DateTime.Now:yyyyMMdd_HHmmss}";
            CurrentState = GameState.Playing;
            
            // 시간 초기화
            if (_timeManager != null)
            {
                _timeManager.Initialize(1, 6, 0); // Day 1, 06:00
            }
            
            // 게임 씬 로드
            LoadGameScene();
            
            EventBus.Publish(new GameStartedEvent
            {
                IsNewGame = true,
                SaveName = CurrentSaveName
            });
            
            Debug.Log($"[GameManager] New game started: {CurrentSaveName}");
        }
        
        /// <summary>
        /// 저장된 게임 불러오기
        /// </summary>
        public void LoadGame(string saveName)
        {
            CurrentSaveName = saveName;
            CurrentState = GameState.Loading;
            
            // 게임 씬 로드
            LoadGameScene(() =>
            {
                // 씬 로드 후 저장 데이터 불러오기
                if (_saveSystem != null)
                {
                    bool success = _saveSystem.LoadGame(saveName);
                    if (success)
                    {
                        CurrentState = GameState.Playing;
                        Debug.Log($"[GameManager] Game loaded: {saveName}");
                    }
                    else
                    {
                        CurrentState = GameState.MainMenu;
                        Debug.LogError($"[GameManager] Failed to load game: {saveName}");
                    }
                }
            });
        }
        
        /// <summary>
        /// 게임 저장
        /// </summary>
        public void SaveGame(string saveName = null)
        {
            if (CurrentState != GameState.Playing)
            {
                Debug.LogWarning("[GameManager] Cannot save in current state: " + CurrentState);
                return;
            }
            
            string nameToSave = saveName ?? CurrentSaveName ?? "quicksave";
            
            if (_saveSystem != null)
            {
                _saveSystem.SaveGame(nameToSave);
                CurrentSaveName = nameToSave;
            }
        }
        
        /// <summary>
        /// 게임 일시정지
        /// </summary>
        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
                Time.timeScale = 0f;
                
                if (_timeManager != null)
                {
                    _timeManager.Pause();
                }
                
                Debug.Log("[GameManager] Game paused");
            }
        }
        
        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Playing;
                Time.timeScale = 1f;
                
                if (_timeManager != null)
                {
                    _timeManager.Resume();
                }
                
                Debug.Log("[GameManager] Game resumed");
            }
        }
        
        /// <summary>
        /// 메인 메뉴로 돌아가기
        /// </summary>
        public void ReturnToMainMenu()
        {
            CurrentState = GameState.MainMenu;
            CurrentSaveName = null;
            Time.timeScale = 1f;
            
            // 씬 컨테이너 정리
            DIContainer.ClearAll();
            
            EventBus.Publish(new ReturnToMainMenuEvent());
            
            SceneManager.LoadScene(_mainMenuSceneName);
            Debug.Log("[GameManager] Returned to main menu");
        }
        
        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting game");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        /// <summary>
        /// 게임 씬 로드
        /// </summary>
        private void LoadGameScene(Action onComplete = null)
        {
            var asyncLoad = SceneManager.LoadSceneAsync(_gameSceneName);
            
            if (asyncLoad != null && onComplete != null)
            {
                asyncLoad.completed += (_) =>
                {
                    // 씬 로드 후 EventBus 초기화
                    EventBus.Clear();
                    
                    // 씬 로드 후 DI 주입
                    InjectSceneDependencies();
                    onComplete?.Invoke();
                };
            }
            else if (onComplete != null)
            {
                // 즉시 실행 (이미 로드된 경우)
                EventBus.Clear();
                InjectSceneDependencies();
                onComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// 씬 의존성 주입
        /// </summary>
        private void InjectSceneDependencies()
        {
            // 씬 내의 모든 MonoBehaviour에 DI 주입
            var monoBehaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in monoBehaviours)
            {
                DIContainer.Inject(mb);
            }
            
            Debug.Log("[GameManager] Scene dependencies injected");
        }
        
        /// <summary>
        /// 플레이어 사망 처리
        /// </summary>
        public void OnPlayerDied(string deathReason)
        {
            Debug.Log($"[GameManager] Player died: {deathReason}");
            
            CurrentState = GameState.Dead;
            
            SaveGame("death_autosave");
            
            EventBus.Publish(new PlayerDiedEvent
            {
                DeathReason = deathReason
            });
            
            int currentDay = _timeManager?.CurrentDay ?? 1;
            
            if (currentDay <= 3)
            {
                Invoke(nameof(ShowRespawnUI), 2f);
            }
            else
            {
                Invoke(nameof(GameOver), 3f);
            }
        }
        
        /// <summary>
        /// 부활 UI 표시
        /// </summary>
        private void ShowRespawnUI()
        {
            CurrentState = GameState.Playing;
            
            EventBus.Publish(new PlayerRespawnedEvent
            {
                RespawnPosition = GetRespawnPosition()
            });
            
            Debug.Log("[GameManager] Player respawned");
        }
        
        /// <summary>
        /// 리스폰 위치 반환
        /// </summary>
        private Vector3 GetRespawnPosition()
        {
            return Vector3.zero;
        }
        
        /// <summary>
        /// 게임 오버
        /// </summary>
        public void GameOver()
        {
            CurrentState = GameState.GameOver;
            Debug.Log("[GameManager] Game Over");
            
            EventBus.Publish(new GameOverEvent
            {
                DeathReason = "게임 오버"
            });
        }
        
        /// <summary>
        /// 배 건설 완료 (게임 클리어)
        /// </summary>
        public void OnBoatBuilt()
        {
            CurrentState = GameState.Victory;
            Debug.Log("[GameManager] Victory! Boat built!");
            
            EventBus.Publish(new GameClearEvent());
            
            // 2초 후 메인 메뉴로 돌아가기
            Invoke(nameof(ReturnToMainMenu), 2f);
        }
        
        private void OnItemCrafted(ItemCraftedEvent evt)
        {
            if (evt.ResultItemId == "boat")
            {
                OnBoatBuilt();
            }
        }
        
        /// <summary>
        /// 디버그: 현재 상태 출력
        /// </summary>
        [ContextMenu("Debug State")]
        private void DebugState()
        {
            Debug.Log($"[GameManager] State: {CurrentState}, Save: {CurrentSaveName}");
        }
        
        public class GameClearEvent
        {
        }
    }
    
    /// <summary>
    /// 게임 상태 열거형
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Loading,
        GameOver,
        Dead,
        Victory
    }
}
