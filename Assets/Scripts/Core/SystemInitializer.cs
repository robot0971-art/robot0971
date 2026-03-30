using System;
using System.Threading.Tasks;
using UnityEngine;
using DI;

namespace SunnysideIsland.Core
{
    /// <summary>
    /// 시스템 초기화 기본 클래스
    /// </summary>
    public abstract class SystemInitializer : ScriptableObject
    {
        [Header("=== Initializer Info ===")]
        [SerializeField] private string _initializerName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        // 진행률 (0.0 ~ 1.0)
        public float Progress { get; protected set; }

        // 현재 상태 메시지
        public string StatusMessage { get; protected set; }

        // 초기화 완료 여부
        public bool IsInitialized { get; protected set; }

        // 초기화 중 에러 발생 여부
        public bool HasError { get; protected set; }

        // 에러 메시지
        public string ErrorMessage { get; protected set; }

        // 진행률 변경 이벤트
        public event Action<float> OnProgressChanged;

        // 상태 변경 이벤트
        public event Action<string> OnStatusChanged;

        // 초기화 완료 이벤트
        public event Action OnInitialized;

        // 에러 발생 이벤트
        public event Action<string> OnError;

        /// <summary>
        /// 동기 초기화 (간단한 시스템용)
        /// </summary>
        public virtual void Initialize()
        {
            try
            {
                StatusMessage = $"Initializing {_initializerName}...";
                IsInitialized = false;
                HasError = false;
                Progress = 0f;

                // 초기화 로직 실행
                DoInitialize();

                Progress = 1f;
                IsInitialized = true;
                StatusMessage = $"{_initializerName} initialized";

                OnInitialized?.Invoke();
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }

        /// <summary>
        /// 비동기 초기화 (복잡한 시스템용)
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            try
            {
                StatusMessage = $"Initializing {_initializerName}...";
                IsInitialized = false;
                HasError = false;
                Progress = 0f;

                // 비동기 초기화 로직 실행
                await DoInitializeAsync();

                Progress = 1f;
                IsInitialized = true;
                StatusMessage = $"{_initializerName} initialized";

                OnInitialized?.Invoke();
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }

        /// <summary>
        /// 실제 초기화 로직 (동기) - 오버라이드해서 사용
        /// </summary>
        protected virtual void DoInitialize()
        {
            // 기본 구현: 비동기 메서드를 동기로 래핑
            DoInitializeAsync().Wait();
        }

        /// <summary>
        /// 실제 초기화 로직 (비동기) - 오버라이드해서 사용
        /// </summary>
        protected virtual Task DoInitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 진행률 업데이트
        /// </summary>
        protected void SetProgress(float progress)
        {
            Progress = Mathf.Clamp01(progress);
            OnProgressChanged?.Invoke(Progress);
        }

        /// <summary>
        /// 상태 메시지 업데이트
        /// </summary>
        protected void SetStatus(string status)
        {
            StatusMessage = status;
            OnStatusChanged?.Invoke(status);
        }

        /// <summary>
        /// 에러 처리
        /// </summary>
        protected void HandleError(Exception e)
        {
            HasError = true;
            ErrorMessage = e.Message;
            StatusMessage = $"Error: {e.Message}";

            Debug.LogError($"[{_initializerName}] Initialization error: {e.Message}");

            OnError?.Invoke(e.Message);
        }

        /// <summary>
        /// 리셋
        /// </summary>
        public virtual void Reset()
        {
            Progress = 0f;
            StatusMessage = string.Empty;
            IsInitialized = false;
            HasError = false;
            ErrorMessage = string.Empty;
        }

        private void OnEnable()
        {
            Reset();
        }
    }

    /// <summary>
    /// DI를 사용하는 시스템 이니셜라이저 기본 클래스
    /// </summary>
    public abstract class DISystemInitializer : SystemInitializer
    {
        /// <summary>
        /// DI 컨테이너에 서비스 등록
        /// </summary>
        protected virtual void RegisterServices()
        {
        }

        /// <summary>
        /// 서비스 인스턴스 해결
        /// </summary>
        protected T Resolve<T>() where T : class
        {
            return DIContainer.Resolve<T>();
        }

        /// <summary>
        /// 서비스 등록
        /// </summary>
        protected void Register<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            DIContainer.Global.Register<TInterface, TImplementation>();
        }

        /// <summary>
        /// 인스턴스 등록
        /// </summary>
        protected void RegisterInstance<TInterface>(TInterface instance)
        {
            DIContainer.Global.RegisterInstance(instance);
        }

        protected override void DoInitialize()
        {
            RegisterServices();
            base.DoInitialize();
        }

        protected override async Task DoInitializeAsync()
        {
            RegisterServices();
            await base.DoInitializeAsync();
        }
    }
}