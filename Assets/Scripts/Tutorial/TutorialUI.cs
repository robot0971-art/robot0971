using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.UI;

namespace SunnysideIsland.Tutorial
{
    /// <summary>
    /// 튜토리얼 UI 패널
    /// </summary>
    public class TutorialUI : UIPanel
    {
        [Header("=== UI References ===")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _stepCounterText;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private GameObject _highlightOverlay;

        [Header("=== Highlight Settings ===")]
        [SerializeField] private Image _highlightMask;
        [SerializeField] private float _highlightPadding = 10f;
        [SerializeField] private Color _highlightBackgroundColor = new Color(0f, 0f, 0f, 0.7f);

        [Header("=== Animation ===")]
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.2f;

        private TutorialManager _tutorialManager;
        private TutorialStep _currentStep;
        private GameObject _currentHighlightTarget;
        private bool _isInitialized;

        protected override void Awake()
        {
            base.Awake();
            InitializeComponents();
        }

        private void Start()
        {
            InitializeTutorialManager();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextButtonClick);
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.AddListener(OnSkipButtonClick);
            }

            if (_highlightOverlay != null)
            {
                _highlightOverlay.SetActive(false);
            }

            if (_highlightMask != null)
            {
                _highlightMask.color = _highlightBackgroundColor;
            }
        }

        /// <summary>
        /// 튜토리얼 매니저 초기화
        /// </summary>
        private void InitializeTutorialManager()
        {
            _tutorialManager = TutorialManager.Instance;

            if (_tutorialManager != null)
            {
                _tutorialManager.OnStepStarted += OnStepStarted;
                _tutorialManager.OnStepCompleted += OnStepCompleted;
                _tutorialManager.OnTutorialStarted += OnTutorialStarted;
                _tutorialManager.OnTutorialCompleted += OnTutorialCompleted;
            }
            else
            {
                Debug.LogWarning("[TutorialUI] TutorialManager not found");
            }
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            EventBus.Subscribe<TutorialStartedEvent>(OnTutorialStartedEvent);
            EventBus.Subscribe<TutorialCompletedEvent>(OnTutorialCompletedEvent);
            EventBus.Subscribe<TutorialHighlightEvent>(OnHighlightEvent);
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<TutorialStartedEvent>(OnTutorialStartedEvent);
            EventBus.Unsubscribe<TutorialCompletedEvent>(OnTutorialCompletedEvent);
            EventBus.Unsubscribe<TutorialHighlightEvent>(OnHighlightEvent);

            if (_tutorialManager != null)
            {
                _tutorialManager.OnStepStarted -= OnStepStarted;
                _tutorialManager.OnStepCompleted -= OnStepCompleted;
                _tutorialManager.OnTutorialStarted -= OnTutorialStarted;
                _tutorialManager.OnTutorialCompleted -= OnTutorialCompleted;
            }
        }

        #region Event Handlers

        private void OnTutorialStartedEvent(TutorialStartedEvent evt)
        {
            UpdateStepCounter(evt.TotalSteps, 1);
        }

        private void OnTutorialCompletedEvent(TutorialCompletedEvent evt)
        {
            ClearHighlight();
            Close();
        }

        private void OnHighlightEvent(TutorialHighlightEvent evt)
        {
            if (string.IsNullOrEmpty(evt.TargetPath))
            {
                return;
            }

            var target = GameObject.Find(evt.TargetPath);
            if (target != null)
            {
                HighlightTarget(target);
            }
        }

        private void OnTutorialStarted(TutorialData data)
        {
            Open();
        }

        private void OnTutorialCompleted(TutorialData data)
        {
            ClearHighlight();
            Close();
        }

        private void OnStepStarted(TutorialStep step)
        {
            _currentStep = step;
            UpdateUI(step);
        }

        private void OnStepCompleted(TutorialStep step)
        {
            if (_tutorialManager != null)
            {
                UpdateStepCounter(_tutorialManager.CurrentTutorial?.TotalSteps ?? 0, _tutorialManager.CurrentStepIndex + 1);
            }
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI(TutorialStep step)
        {
            if (step == null)
            {
                return;
            }

            if (_titleText != null)
            {
                _titleText.text = step.title;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = step.description;
            }

            if (_nextButton != null)
            {
                bool showNext = step.requiredAction == TutorialActionType.Click || step.autoAdvance;
                _nextButton.gameObject.SetActive(showNext);
            }

            if (_tutorialManager != null && _tutorialManager.CurrentTutorial != null)
            {
                UpdateStepCounter(_tutorialManager.CurrentTutorial.TotalSteps, _tutorialManager.CurrentStepIndex + 1);
            }
        }

        /// <summary>
        /// 단계 카운터 업데이트
        /// </summary>
        private void UpdateStepCounter(int totalSteps, int currentStep)
        {
            if (_stepCounterText != null && totalSteps > 0)
            {
                _stepCounterText.text = $"{currentStep} / {totalSteps}";
            }
        }

        #endregion

        #region Highlight

        /// <summary>
        /// 타겟 하이라이트
        /// </summary>
        public void HighlightTarget(GameObject target)
        {
            if (target == null || _highlightOverlay == null)
            {
                return;
            }

            _currentHighlightTarget = target;
            _highlightOverlay.SetActive(true);

            UpdateHighlightPosition();

            StartCoroutine(AnimateHighlight(true));
        }

        /// <summary>
        /// 하이라이트 위치 업데이트
        /// </summary>
        private void UpdateHighlightPosition()
        {
            if (_currentHighlightTarget == null || _highlightMask == null)
            {
                return;
            }

            var rectTransform = _currentHighlightTarget.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Vector3 bottomLeft = corners[0];
            Vector3 topRight = corners[2];

            float width = topRight.x - bottomLeft.x + _highlightPadding * 2;
            float height = topRight.y - bottomLeft.y + _highlightPadding * 2;

            var highlightRect = _highlightMask.rectTransform;
            if (highlightRect != null)
            {
                highlightRect.sizeDelta = new Vector2(width, height);

                Vector3 center = (bottomLeft + topRight) / 2;
                highlightRect.position = center;
            }
        }

        /// <summary>
        /// 하이라이트 초기화
        /// </summary>
        public void ClearHighlight()
        {
            _currentHighlightTarget = null;

            if (_highlightOverlay != null)
            {
                StartCoroutine(AnimateHighlight(false));
            }
        }

        /// <summary>
        /// 하이라이트 애니메이션
        /// </summary>
        private System.Collections.IEnumerator AnimateHighlight(bool show)
        {
            if (_highlightMask == null)
            {
                yield break;
            }

            float duration = show ? _fadeInDuration : _fadeOutDuration;
            float elapsed = 0f;

            Color startColor = _highlightMask.color;
            Color endColor = show
                ? _highlightBackgroundColor
                : new Color(_highlightBackgroundColor.r, _highlightBackgroundColor.g, _highlightBackgroundColor.b, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                _highlightMask.color = Color.Lerp(startColor, endColor, t);

                yield return null;
            }

            _highlightMask.color = endColor;

            if (!show && _highlightOverlay != null)
            {
                _highlightOverlay.SetActive(false);
            }
        }

        #endregion

        #region Button Handlers

        private void OnNextButtonClick()
        {
            if (_tutorialManager != null)
            {
                _tutorialManager.CompleteCurrentStep();
            }
        }

        private void OnSkipButtonClick()
        {
            if (_tutorialManager != null)
            {
                _tutorialManager.SkipTutorial();
            }
        }

        #endregion

        protected override void OnOpened()
        {
            base.OnOpened();

            if (_tutorialManager != null && _tutorialManager.IsTutorialActive)
            {
                var currentStep = _tutorialManager.CurrentTutorial?.GetStep(_tutorialManager.CurrentStepIndex);
                if (currentStep != null)
                {
                    UpdateUI(currentStep);
                }
            }
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            ClearHighlight();
        }

        public override void OnBackButton()
        {
            if (_skipButton != null && _skipButton.gameObject.activeSelf)
            {
                OnSkipButtonClick();
            }
            else
            {
                base.OnBackButton();
            }
        }
    }
}