using System;
using System.Collections.Generic;
using UnityEngine;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.UI;

namespace SunnysideIsland.Tutorial
{
    /// <summary>
    /// 튜토리얼 진행 상태 저장 데이터
    /// </summary>
    [Serializable]
    public class TutorialSaveData
    {
        public List<string> completedTutorials = new List<string>();
        public Dictionary<string, int> tutorialProgress = new Dictionary<string, int>();
    }

    /// <summary>
    /// 튜토리얼 관리 매니저
    /// </summary>
    public class TutorialManager : MonoBehaviour, ISaveable
    {
        public static TutorialManager Instance { get; private set; }

        [Header("=== Tutorial Data ===")]
        [SerializeField] private List<TutorialData> _tutorialDataList = new List<TutorialData>();

        [Header("=== Settings ===")]
        [SerializeField] private bool _enableTutorial = true;
        [SerializeField] private float _highlightAnimationDuration = 0.3f;

        private readonly Dictionary<string, TutorialData> _tutorialDictionary = new Dictionary<string, TutorialData>();
        private readonly Dictionary<string, int> _tutorialProgress = new Dictionary<string, int>();
        private readonly HashSet<string> _completedTutorials = new HashSet<string>();

        private TutorialData _currentTutorial;
        private int _currentStepIndex;
        private bool _isTutorialActive;
        private float _waitTimer;
        private bool _isWaitingForAction;

        public string SaveKey => "TutorialManager";
        public bool IsTutorialActive => _isTutorialActive;
        public TutorialData CurrentTutorial => _currentTutorial;
        public int CurrentStepIndex => _currentStepIndex;

        public event Action<TutorialStep> OnStepStarted;
        public event Action<TutorialStep> OnStepCompleted;
        public event Action<TutorialData> OnTutorialStarted;
        public event Action<TutorialData> OnTutorialCompleted;

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
            InitializeTutorialData();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (!_isTutorialActive || _currentTutorial == null)
            {
                return;
            }

            UpdateCurrentStep();
        }

        /// <summary>
        /// 튜토리얼 데이터 초기화
        /// </summary>
        private void InitializeTutorialData()
        {
            _tutorialDictionary.Clear();

            foreach (var tutorialData in _tutorialDataList)
            {
                if (tutorialData != null && !string.IsNullOrEmpty(tutorialData.tutorialId))
                {
                    if (_tutorialDictionary.ContainsKey(tutorialData.tutorialId))
                    {
                        Debug.LogWarning($"[TutorialManager] Duplicate tutorial ID: {tutorialData.tutorialId}");
                        continue;
                    }
                    _tutorialDictionary[tutorialData.tutorialId] = tutorialData;
                }
            }

            Debug.Log($"[TutorialManager] Initialized {_tutorialDictionary.Count} tutorials");
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            EventBus.Subscribe<TutorialStepCompletedEvent>(OnTutorialStepCompletedEvent);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<TutorialStepCompletedEvent>(OnTutorialStepCompletedEvent);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        /// <summary>
        /// 게임 시작 이벤트 처리
        /// </summary>
        private void OnGameStarted(GameStartedEvent evt)
        {
            if (!evt.IsNewGame)
            {
                return;
            }

            foreach (var tutorial in _tutorialDataList)
            {
                if (tutorial != null && tutorial.autoStart && !IsTutorialCompleted(tutorial.tutorialId))
                {
                    StartTutorial(tutorial.tutorialId);
                    break;
                }
            }
        }

        /// <summary>
        /// 튜토리얼 단계 완료 이벤트 처리
        /// </summary>
        private void OnTutorialStepCompletedEvent(TutorialStepCompletedEvent evt)
        {
            if (_currentTutorial == null || _currentTutorial.tutorialId != evt.TutorialId)
            {
                return;
            }

            if (_currentStepIndex == evt.StepIndex)
            {
                NextStep();
            }
        }

        /// <summary>
        /// 현재 단계 업데이트
        /// </summary>
        private void UpdateCurrentStep()
        {
            if (_currentTutorial == null || !_isWaitingForAction)
            {
                return;
            }

            var currentStep = _currentTutorial.GetStep(_currentStepIndex);
            if (currentStep == null)
            {
                return;
            }

            switch (currentStep.requiredAction)
            {
                case TutorialActionType.Wait:
                    _waitTimer -= Time.unscaledDeltaTime;
                    if (_waitTimer <= 0f)
                    {
                        CompleteCurrentStep();
                    }
                    break;

                case TutorialActionType.PressKey:
                    if (!string.IsNullOrEmpty(currentStep.actionTarget))
                    {
                        KeyCode keyCode;
                        if (Enum.TryParse(currentStep.actionTarget, out keyCode))
                        {
                            if (Input.GetKeyDown(keyCode))
                            {
                                CompleteCurrentStep();
                            }
                        }
                        else if (Input.GetButtonDown(currentStep.actionTarget))
                        {
                            CompleteCurrentStep();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 튜토리얼 시작
        /// </summary>
        public bool StartTutorial(string tutorialId)
        {
            if (!_enableTutorial)
            {
                Debug.Log("[TutorialManager] Tutorials are disabled");
                return false;
            }

            if (IsTutorialCompleted(tutorialId))
            {
                Debug.Log($"[TutorialManager] Tutorial {tutorialId} already completed");
                return false;
            }

            if (!_tutorialDictionary.TryGetValue(tutorialId, out var tutorialData))
            {
                Debug.LogError($"[TutorialManager] Tutorial not found: {tutorialId}");
                return false;
            }

            if (!string.IsNullOrEmpty(tutorialData.prerequisiteTutorialId))
            {
                if (!IsTutorialCompleted(tutorialData.prerequisiteTutorialId))
                {
                    Debug.LogWarning($"[TutorialManager] Prerequisite tutorial not completed: {tutorialData.prerequisiteTutorialId}");
                    return false;
                }
            }

            _currentTutorial = tutorialData;
            _currentStepIndex = _tutorialProgress.TryGetValue(tutorialId, out var progress) ? progress : 0;
            _isTutorialActive = true;
            _isWaitingForAction = false;

            OnTutorialStarted?.Invoke(_currentTutorial);

            EventBus.Publish(new TutorialStartedEvent
            {
                TutorialId = tutorialId,
                TutorialName = tutorialData.tutorialName,
                TotalSteps = tutorialData.TotalSteps
            });

            ShowCurrentStep();

            Debug.Log($"[TutorialManager] Started tutorial: {tutorialId}");

            return true;
        }

        /// <summary>
        /// 현재 단계 표시
        /// </summary>
        private void ShowCurrentStep()
        {
            if (_currentTutorial == null)
            {
                return;
            }

            var currentStep = _currentTutorial.GetStep(_currentStepIndex);
            if (currentStep == null)
            {
                CompleteTutorial();
                return;
            }

            _isWaitingForAction = true;

            if (currentStep.requiredAction == TutorialActionType.Wait)
            {
                _waitTimer = currentStep.delay;
            }

            OnStepStarted?.Invoke(currentStep);

            HighlightTargets(currentStep.highlightTargets);
        }

        /// <summary>
        /// 타겟 하이라이트
        /// </summary>
        private void HighlightTargets(List<string> targetPaths)
        {
            if (targetPaths == null || targetPaths.Count == 0)
            {
                return;
            }

            foreach (var path in targetPaths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var target = GameObject.Find(path);
                if (target != null)
                {
                    EventBus.Publish(new TutorialHighlightEvent
                    {
                        TargetPath = path,
                        Duration = _highlightAnimationDuration
                    });
                }
            }
        }

        /// <summary>
        /// 다음 단계로 진행
        /// </summary>
        public void NextStep()
        {
            if (!_isTutorialActive || _currentTutorial == null)
            {
                return;
            }

            var currentStep = _currentTutorial.GetStep(_currentStepIndex);
            if (currentStep == null)
            {
                CompleteTutorial();
                return;
            }

            OnStepCompleted?.Invoke(currentStep);

            _tutorialProgress[_currentTutorial.tutorialId] = _currentStepIndex + 1;
            _currentStepIndex++;

            ShowCurrentStep();
        }

        /// <summary>
        /// 현재 단계 완료
        /// </summary>
        public void CompleteCurrentStep()
        {
            if (!_isTutorialActive || _currentTutorial == null)
            {
                return;
            }

            var currentStep = _currentTutorial.GetStep(_currentStepIndex);
            if (currentStep == null)
            {
                return;
            }

            EventBus.Publish(new TutorialStepCompletedEvent
            {
                TutorialId = _currentTutorial.tutorialId,
                StepId = currentStep.stepId,
                StepIndex = _currentStepIndex
            });

            if (currentStep.autoAdvance)
            {
                Invoke(nameof(NextStep), currentStep.advanceDelay);
            }
        }

        /// <summary>
        /// 특정 단계로 이동
        /// </summary>
        public bool GoToStep(int stepIndex)
        {
            if (!_isTutorialActive || _currentTutorial == null)
            {
                return false;
            }

            if (stepIndex < 0 || stepIndex >= _currentTutorial.TotalSteps)
            {
                Debug.LogWarning($"[TutorialManager] Invalid step index: {stepIndex}");
                return false;
            }

            _currentStepIndex = stepIndex;
            _tutorialProgress[_currentTutorial.tutorialId] = stepIndex;
            _isWaitingForAction = false;

            ShowCurrentStep();

            return true;
        }

        /// <summary>
        /// 튜토리얼 완료
        /// </summary>
        private void CompleteTutorial()
        {
            if (_currentTutorial == null)
            {
                return;
            }

            var completedTutorial = _currentTutorial;
            string tutorialId = completedTutorial.tutorialId;

            _completedTutorials.Add(tutorialId);
            _tutorialProgress.Remove(tutorialId);

            _currentTutorial = null;
            _currentStepIndex = 0;
            _isTutorialActive = false;
            _isWaitingForAction = false;

            GrantRewards(completedTutorial.rewards);

            OnTutorialCompleted?.Invoke(completedTutorial);

            EventBus.Publish(new TutorialCompletedEvent
            {
                TutorialId = tutorialId,
                TutorialName = completedTutorial.tutorialName
            });

            Debug.Log($"[TutorialManager] Completed tutorial: {tutorialId}");
        }

        /// <summary>
        /// 튜토리얼 건너뛰기
        /// </summary>
        public void SkipTutorial()
        {
            if (!_isTutorialActive || _currentTutorial == null)
            {
                return;
            }

            Debug.Log($"[TutorialManager] Skipping tutorial: {_currentTutorial.tutorialId}");

            CompleteTutorial();
        }

        /// <summary>
        /// 보상 지급
        /// </summary>
        private void GrantRewards(List<TutorialReward> rewards)
        {
            if (rewards == null || rewards.Count == 0)
            {
                return;
            }

            foreach (var reward in rewards)
            {
                switch (reward.rewardType)
                {
                    case TutorialRewardType.Currency:
                        EventBus.Publish(new TutorialRewardGrantedEvent
                        {
                            RewardType = reward.rewardType,
                            RewardId = reward.rewardId,
                            Amount = reward.amount
                        });
                        Debug.Log($"[TutorialManager] Granted currency: {reward.rewardId} x{reward.amount}");
                        break;

                    case TutorialRewardType.Item:
                        EventBus.Publish(new TutorialRewardGrantedEvent
                        {
                            RewardType = reward.rewardType,
                            RewardId = reward.rewardId,
                            Amount = reward.amount
                        });
                        Debug.Log($"[TutorialManager] Granted item: {reward.rewardId} x{reward.amount}");
                        break;

                    default:
                        Debug.Log($"[TutorialManager] Reward type {reward.rewardType} not implemented");
                        break;
                }
            }
        }

        /// <summary>
        /// 튜토리얼 완료 여부 확인
        /// </summary>
        public bool IsTutorialCompleted(string tutorialId)
        {
            return !string.IsNullOrEmpty(tutorialId) && _completedTutorials.Contains(tutorialId);
        }

        /// <summary>
        /// 튜토리얼 진행도 반환
        /// </summary>
        public int GetTutorialProgress(string tutorialId)
        {
            return _tutorialProgress.TryGetValue(tutorialId, out var progress) ? progress : 0;
        }

        /// <summary>
        /// 튜토리얼 데이터 반환
        /// </summary>
        public TutorialData GetTutorialData(string tutorialId)
        {
            _tutorialDictionary.TryGetValue(tutorialId, out var data);
            return data;
        }

        /// <summary>
        /// 튜토리얼 활성화/비활성화
        /// </summary>
        public void SetTutorialEnabled(bool enabled)
        {
            _enableTutorial = enabled;
        }

        /// <summary>
        /// 튜토리얼 데이터 추가
        /// </summary>
        public void RegisterTutorialData(TutorialData tutorialData)
        {
            if (tutorialData == null || string.IsNullOrEmpty(tutorialData.tutorialId))
            {
                return;
            }

            if (!_tutorialDictionary.ContainsKey(tutorialData.tutorialId))
            {
                _tutorialDictionary[tutorialData.tutorialId] = tutorialData;
                _tutorialDataList.Add(tutorialData);
            }
        }

        #region ISaveable

        public object GetSaveData()
        {
            return new TutorialSaveData
            {
                completedTutorials = new List<string>(_completedTutorials),
                tutorialProgress = new Dictionary<string, int>(_tutorialProgress)
            };
        }

        public void LoadSaveData(object data)
        {
            if (data is TutorialSaveData saveData)
            {
                _completedTutorials.Clear();
                _tutorialProgress.Clear();

                foreach (var tutorialId in saveData.completedTutorials)
                {
                    _completedTutorials.Add(tutorialId);
                }

                foreach (var kvp in saveData.tutorialProgress)
                {
                    _tutorialProgress[kvp.Key] = kvp.Value;
                }

                Debug.Log($"[TutorialManager] Loaded tutorial progress: {_completedTutorials.Count} completed");
            }
        }

        #endregion
    }

    #region Events

    /// <summary>
    /// 튜토리얼 시작 이벤트
    /// </summary>
    public class TutorialStartedEvent
    {
        public string TutorialId { get; set; }
        public string TutorialName { get; set; }
        public int TotalSteps { get; set; }
    }

    /// <summary>
    /// 튜토리얼 완료 이벤트
    /// </summary>
    public class TutorialCompletedEvent
    {
        public string TutorialId { get; set; }
        public string TutorialName { get; set; }
    }

    /// <summary>
    /// 튜토리얼 단계 완료 이벤트
    /// </summary>
    public class TutorialStepCompletedEvent
    {
        public string TutorialId { get; set; }
        public string StepId { get; set; }
        public int StepIndex { get; set; }
    }

    /// <summary>
    /// 튜토리얼 하이라이트 이벤트
    /// </summary>
    public class TutorialHighlightEvent
    {
        public string TargetPath { get; set; }
        public float Duration { get; set; }
    }

    /// <summary>
    /// 튜토리얼 보상 지급 이벤트
    /// </summary>
    public class TutorialRewardGrantedEvent
    {
        public TutorialRewardType RewardType { get; set; }
        public string RewardId { get; set; }
        public int Amount { get; set; }
    }

    #endregion
}