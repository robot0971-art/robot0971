using System;
using UnityEngine;

namespace SunnysideIsland.Core
{
    /// <summary>
    /// 초기화 스텝을 정의하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "InitializationStep", menuName = "Sunnyside Island/Core/Initialization Step")]
    public sealed class InitializationStep : ScriptableObject
    {
        [Header("=== Step Info ===")]
        [SerializeField] private string _stepName;
        [SerializeField] private int _priority;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("=== Dependencies ===")]
        [SerializeField] private string[] _dependencies;

        [Header("=== Initializer ===")]
        [SerializeField] private SystemInitializer _initializer;

        // 완료 여부
        public bool IsComplete { get; private set; }

        // 프로퍼티
        public string StepName => _stepName;
        public int Priority => _priority;
        public string Description => _description;
        public string[] Dependencies => _dependencies;
        public SystemInitializer Initializer => _initializer;

        /// <summary>
        /// 스텝 실행
        /// </summary>
        public void Execute()
        {
            if (IsComplete)
            {
                Debug.LogWarning($"[InitializationStep] Step already complete: {_stepName}");
                return;
            }

            if (_initializer != null)
            {
                _initializer.Initialize();
            }

            SetComplete();
        }

        /// <summary>
        /// 완료 상태로 설정
        /// </summary>
        public void SetComplete()
        {
            IsComplete = true;
        }

        /// <summary>
        /// 상태 리셋
        /// </summary>
        public void Reset()
        {
            IsComplete = false;
        }

        /// <summary>
        /// 데이터 설정 (런타임 생성용)
        /// </summary>
        public void SetData(string name, int priority)
        {
            _stepName = name;
            _priority = priority;
        }

        /// <summary>
        /// 이니셜라이저 설정
        /// </summary>
        public void SetInitializer(SystemInitializer initializer)
        {
            _initializer = initializer;
        }

        /// <summary>
        /// 의존성 설정
        /// </summary>
        public void SetDependencies(string[] dependencies)
        {
            _dependencies = dependencies;
        }

        private void OnEnable()
        {
            // 에셋 로드 시 상태 초기화
            IsComplete = false;
        }
    }
}