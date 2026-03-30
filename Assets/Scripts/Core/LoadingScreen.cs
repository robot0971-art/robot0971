using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SunnysideIsland.Core
{
    /// <summary>
    /// 로딩 화면 UI 컴포넌트
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        [Header("=== UI References ===")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _tipText;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("=== Settings ===")]
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private float _tipChangeInterval = 5f;

        [Header("=== Localization Keys ===")]
        [SerializeField] private string[] _tipLocalizationKeys;

        // 상태
        public bool IsVisible { get; private set; }
        public float CurrentProgress { get; private set; }

        // 팁 인덱스
        private int _currentTipIndex;
        private float _tipTimer;

        // 페이드 관련
        private bool _isFading;
        private float _fadeStartTime;
        private float _fadeStartAlpha;
        private float _fadeTargetAlpha;

        // 완료 콜백
        private TaskCompletionSource<bool> _hideTaskCompletionSource;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            // 초기 상태 설정
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_errorText != null)
            {
                _errorText.gameObject.SetActive(false);
            }

            if (_retryButton != null)
            {
                _retryButton.gameObject.SetActive(false);
                _retryButton.onClick.AddListener(OnRetryClicked);
            }

            IsVisible = false;
        }

        private void Update()
        {
            UpdateFade();
            UpdateTipRotation();
        }

        /// <summary>
        /// 로딩 화면 표시
        /// </summary>
        public void Show()
        {
            if (IsVisible)
            {
                return;
            }

            IsVisible = true;
            gameObject.SetActive(true);

            // 초기화
            SetProgress(0f);
            SetStatusText("Loading...");
            ShowNextTip();

            if (_errorText != null)
            {
                _errorText.gameObject.SetActive(false);
            }

            if (_retryButton != null)
            {
                _retryButton.gameObject.SetActive(false);
            }

            // 페이드 인
            StartFade(1f);

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// 로딩 화면 숨기기
        /// </summary>
        public void Hide()
        {
            if (!IsVisible)
            {
                return;
            }

            StartFade(0f);
        }

        /// <summary>
        /// 로딩 화면 비동기 숨기기
        /// </summary>
        public async Task HideAsync()
        {
            if (!IsVisible)
            {
                return;
            }

            _hideTaskCompletionSource = new TaskCompletionSource<bool>();
            StartFade(0f);

            await _hideTaskCompletionSource.Task;
        }

        /// <summary>
        /// 진행률 설정
        /// </summary>
        public void SetProgress(float progress)
        {
            CurrentProgress = Mathf.Clamp01(progress);

            if (_progressBar != null)
            {
                _progressBar.value = CurrentProgress;
            }
        }

        /// <summary>
        /// 상태 텍스트 설정
        /// </summary>
        public void SetStatusText(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
            }
        }

        /// <summary>
        /// 팁 텍스트 설정
        /// </summary>
        public void SetTipText(string text)
        {
            if (_tipText != null)
            {
                _tipText.text = text;
            }
        }

        /// <summary>
        /// 에러 표시
        /// </summary>
        public void ShowError(string errorMessage)
        {
            if (_errorText != null)
            {
                _errorText.text = errorMessage;
                _errorText.gameObject.SetActive(true);
            }

            if (_retryButton != null)
            {
                _retryButton.gameObject.SetActive(true);
            }

            SetStatusText("Error");
        }

        /// <summary>
        /// 다음 팁 표시
        /// </summary>
        private void ShowNextTip()
        {
            if (_tipLocalizationKeys == null || _tipLocalizationKeys.Length == 0)
            {
                return;
            }

            // 로컬라이제이션 키에서 텍스트 가져오기 (실제 로컬라이제이션 시스템 필요)
            // 현재는 키 자체를 표시
            string tip = _tipLocalizationKeys[_currentTipIndex];
            SetTipText(tip);

            _currentTipIndex = (_currentTipIndex + 1) % _tipLocalizationKeys.Length;
            _tipTimer = 0f;
        }

        /// <summary>
        /// 팁 회전 업데이트
        /// </summary>
        private void UpdateTipRotation()
        {
            if (!IsVisible || _tipLocalizationKeys == null || _tipLocalizationKeys.Length <= 1)
            {
                return;
            }

            _tipTimer += Time.unscaledDeltaTime;

            if (_tipTimer >= _tipChangeInterval)
            {
                ShowNextTip();
            }
        }

        /// <summary>
        /// 페이드 시작
        /// </summary>
        private void StartFade(float targetAlpha)
        {
            _isFading = true;
            _fadeStartTime = Time.unscaledTime;
            _fadeStartAlpha = _canvasGroup != null ? _canvasGroup.alpha : 0f;
            _fadeTargetAlpha = targetAlpha;
        }

        /// <summary>
        /// 페이드 업데이트
        /// </summary>
        private void UpdateFade()
        {
            if (!_isFading || _canvasGroup == null)
            {
                return;
            }

            float elapsed = Time.unscaledTime - _fadeStartTime;
            float t = Mathf.Clamp01(elapsed / _fadeDuration);

            // 이징 적용 (부드러운 페이드)
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            float newAlpha = Mathf.Lerp(_fadeStartAlpha, _fadeTargetAlpha, easedT);

            _canvasGroup.alpha = newAlpha;

            // 페이드 완료 체크
            if (t >= 1f)
            {
                _isFading = false;
                OnFadeComplete();
            }
        }

        /// <summary>
        /// 페이드 완료 처리
        /// </summary>
        private void OnFadeComplete()
        {
            if (_fadeTargetAlpha <= 0f)
            {
                // 완전히 숨겨짐
                IsVisible = false;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);

                // 비동기 숨기기 완료 알림
                _hideTaskCompletionSource?.TrySetResult(true);
                _hideTaskCompletionSource = null;
            }
        }

        /// <summary>
        /// 재시도 버튼 클릭 핸들러
        /// </summary>
        private void OnRetryClicked()
        {
            // 에러 상태 리셋
            if (_errorText != null)
            {
                _errorText.gameObject.SetActive(false);
            }

            if (_retryButton != null)
            {
                _retryButton.gameObject.SetActive(false);
            }

            // 부트스트래퍼에 재시도 요청
            if (DI.DIContainer.TryResolve<IGameBootstrapper>(out var bootstrapper))
            {
                bootstrapper.RegisterStep(null); // 트리거용
            }
        }

        /// <summary>
        /// 팁 키 설정 (런타임용)
        /// </summary>
        public void SetTipKeys(string[] keys)
        {
            _tipLocalizationKeys = keys;
            _currentTipIndex = 0;
        }

        private void OnDestroy()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(OnRetryClicked);
            }
        }
    }
}