using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using DI;
using SunnysideIsland.Events;

namespace SunnysideIsland.Core
{
    /// <summary>
    /// 게임 부트스트래퍼 - 게임 시작 시 시스템들을 초기화
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour, IGameBootstrapper
    {
        private static GameBootstrapper _instance;
        private static bool _isInitialized;

        [Header("=== Settings ===")]
        [SerializeField] private string _initialSceneName = "MainMenu";
        [SerializeField] private float _minimumLoadingTime = 1f;
        [SerializeField] private bool _showLoadingScreen = true;

        [Header("=== References ===")]
        [SerializeField] private LoadingScreen _loadingScreenPrefab;

        private List<InitializationStep> _steps = new();
        private LoadingScreen _loadingScreen;
        private int _currentStepIndex;
        private float _loadingStartTime;

        // 초기화 진행률
        public float Progress { get; private set; }
        public string CurrentStepName { get; private set; }
        public bool IsInitializing { get; private set; }

        /// <summary>
        /// 런타임 초기화 - 씬 로드 전 자동 실행
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            if (_isInitialized)
            {
                return;
            }

            CreateBootstrapper();
        }

        /// <summary>
        /// 부트스트래퍼 오브젝트 생성
        /// </summary>
        private static void CreateBootstrapper()
        {
            var go = new GameObject("[GameBootstrapper]");
            _instance = go.AddComponent<GameBootstrapper>();
            DontDestroyOnLoad(go);

            _instance.StartInitialization();
        }

        /// <summary>
        /// 초기화 시작
        /// </summary>
        private async void StartInitialization()
        {
            IsInitializing = true;
            _loadingStartTime = Time.realtimeSinceStartup;

            try
            {
                // DI 컨테이너 초기화
                InitializeDIContainer();

                // 로딩 화면 표시
                ShowLoadingScreen();

                // 초기화 스텝 로드
                LoadInitializationSteps();

                // 각 스텝 순차 실행
                await ExecuteStepsAsync();

                // 최소 로딩 시간 보장
                await EnsureMinimumLoadingTime();

                // 초기화 완료
                OnInitializationComplete();
            }
            catch (Exception e)
            {
                OnInitializationError(e);
            }
        }

        /// <summary>
        /// DI 컨테이너 초기화
        /// </summary>
        private void InitializeDIContainer()
        {
            if (DIContainer.Global == null)
            {
                DIContainer.InitializeGlobal();
            }

            // 부트스트래퍼 자체 등록
            DIContainer.Global.RegisterInstance<IGameBootstrapper>(this);

            Debug.Log("[GameBootstrapper] DI Container initialized");
        }

        /// <summary>
        /// 로딩 화면 표시
        /// </summary>
        private void ShowLoadingScreen()
        {
            if (!_showLoadingScreen || _loadingScreenPrefab == null)
            {
                return;
            }

            _loadingScreen = Instantiate(_loadingScreenPrefab, transform);
            _loadingScreen.Show();
        }

        /// <summary>
        /// 초기화 스텝 로드
        /// </summary>
        private void LoadInitializationSteps()
        {
            // Resources에서 모든 InitializationStep 로드
            var stepAssets = Resources.LoadAll<InitializationStep>("InitializationSteps");

            if (stepAssets != null && stepAssets.Length > 0)
            {
                _steps = stepAssets.OrderBy(s => s.Priority).ToList();
            }
            else
            {
                // 기본 스텝 생성 (Resources가 비어있을 경우)
                CreateDefaultSteps();
            }

            Debug.Log($"[GameBootstrapper] Loaded {_steps.Count} initialization steps");
        }

        /// <summary>
        /// 기본 초기화 스텝 생성
        /// </summary>
        private void CreateDefaultSteps()
        {
            _steps = new List<InitializationStep>
            {
                CreateStep("Core Systems", 0),
                CreateStep("Audio System", 100),
                CreateStep("Save System", 200),
                CreateStep("Game Data", 300),
                CreateStep("UI System", 400)
            };
        }

        /// <summary>
        /// 스텝 인스턴스 생성
        /// </summary>
        private InitializationStep CreateStep(string name, int priority)
        {
            var step = ScriptableObject.CreateInstance<InitializationStep>();
            step.SetData(name, priority);
            return step;
        }

        /// <summary>
        /// 모든 스텝 순차 실행
        /// </summary>
        private async Task ExecuteStepsAsync()
        {
            var totalSteps = _steps.Count;

            for (int i = 0; i < totalSteps; i++)
            {
                _currentStepIndex = i;
                var step = _steps[i];
                CurrentStepName = step.StepName;

                // 의존성 체크
                if (!CheckDependencies(step))
                {
                    throw new InvalidOperationException(
                        $"Dependencies not met for step: {step.StepName}");
                }

                // 진행률 업데이트
                Progress = (float)i / totalSteps;
                UpdateLoadingScreen();

                // 스텝 실행
                await ExecuteStepAsync(step);

                step.SetComplete();
                Debug.Log($"[GameBootstrapper] Step completed: {step.StepName}");
            }

            Progress = 1f;
            UpdateLoadingScreen();
        }

        /// <summary>
        /// 단일 스텝 실행
        /// </summary>
        private async Task ExecuteStepAsync(InitializationStep step)
        {
            if (step.Initializer != null)
            {
                await step.Initializer.InitializeAsync();
            }
            else
            {
                // 이니셜라이저가 없으면 딜레이만 줌 (테스트용)
                await Task.Delay(100);
            }
        }

        /// <summary>
        /// 의존성 체크
        /// </summary>
        private bool CheckDependencies(InitializationStep step)
        {
            if (step.Dependencies == null || step.Dependencies.Length == 0)
            {
                return true;
            }

            foreach (var dependencyName in step.Dependencies)
            {
                var dependency = _steps.FirstOrDefault(s => s.StepName == dependencyName);
                if (dependency == null || !dependency.IsComplete)
                {
                    Debug.LogError($"[GameBootstrapper] Missing dependency: {dependencyName}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 로딩 화면 업데이트
        /// </summary>
        private void UpdateLoadingScreen()
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetProgress(Progress);
                _loadingScreen.SetStatusText(CurrentStepName);
            }
        }

        /// <summary>
        /// 최소 로딩 시간 보장
        /// </summary>
        private async Task EnsureMinimumLoadingTime()
        {
            var elapsed = Time.realtimeSinceStartup - _loadingStartTime;
            var remaining = _minimumLoadingTime - elapsed;

            if (remaining > 0)
            {
                await Task.Delay((int)(remaining * 1000));
            }
        }

        /// <summary>
        /// 초기화 완료 처리
        /// </summary>
        private async void OnInitializationComplete()
        {
            _isInitialized = true;
            IsInitializing = false;

            // 완료 이벤트 발생
            EventBus.Publish(new InitializationCompleteEvent
            {
                TotalSteps = _steps.Count,
                TotalTime = Time.realtimeSinceStartup - _loadingStartTime
            });

            Debug.Log("[GameBootstrapper] Initialization complete");

            // 로딩 화면 페이드 아웃
            if (_loadingScreen != null)
            {
                await _loadingScreen.HideAsync();
                Destroy(_loadingScreen.gameObject);
            }

            // 초기 씬 로드
            LoadInitialScene();
        }

        /// <summary>
        /// 초기화 에러 처리
        /// </summary>
        private void OnInitializationError(Exception e)
        {
            IsInitializing = false;

            Debug.LogError($"[GameBootstrapper] Initialization failed: {e.Message}");

            // 에러 이벤트 발생
            EventBus.Publish(new InitializationErrorEvent
            {
                ErrorMessage = e.Message,
                StepName = CurrentStepName,
                Exception = e
            });

            // 로딩 화면에 에러 표시
            if (_loadingScreen != null)
            {
                _loadingScreen.ShowError(e.Message);
            }
        }

        /// <summary>
        /// 초기 씬 로드
        /// </summary>
        private void LoadInitialScene()
        {
            if (string.IsNullOrEmpty(_initialSceneName))
            {
                return;
            }

            // 현재 씬이 이미 로드된 경우 스킵
            var currentScene = SceneManager.GetActiveScene();
            if (currentScene.name == _initialSceneName)
            {
                return;
            }

            SceneManager.LoadScene(_initialSceneName);
        }

        /// <summary>
        /// 초기화 스텝 수동 등록
        /// </summary>
        public void RegisterStep(InitializationStep step)
        {
            if (!_steps.Contains(step))
            {
                _steps.Add(step);
                _steps = _steps.OrderBy(s => s.Priority).ToList();
            }
        }

        /// <summary>
        /// 수동 초기화 (에디터용)
        /// </summary>
        [ContextMenu("Force Initialize")]
        public void ForceInitialize()
        {
            if (!_isInitialized)
            {
                StartInitialization();
            }
        }
    }

    /// <summary>
    /// 게임 부트스트래퍼 인터페이스
    /// </summary>
    public interface IGameBootstrapper
    {
        float Progress { get; }
        string CurrentStepName { get; }
        bool IsInitializing { get; }
        void RegisterStep(InitializationStep step);
    }
}