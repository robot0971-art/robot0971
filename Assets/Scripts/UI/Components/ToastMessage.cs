using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SunnysideIsland.Events;

namespace SunnysideIsland.UI.Components
{
    public class ToastMessage : MonoBehaviour
    {
        public static ToastMessage Instance { get; private set; }

        [Header("=== Settings ===")]
        [SerializeField] private float _defaultDuration = 2f;
        [SerializeField] private float _fadeDuration = 0.3f;

        [Header("=== UI ===")]
        [SerializeField] private GameObject _messagePanel;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _backgroundImage;

        private CanvasGroup _canvasGroup;
        private Coroutine _currentCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_messagePanel != null)
            {
                _canvasGroup = _messagePanel.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = _messagePanel.AddComponent<CanvasGroup>();
                }
            }

            HideImmediate();
        }

        private void Start()
        {
            EventBus.Subscribe<PlacementFailedEvent>(OnPlacementFailed);
            EventBus.Subscribe<ConstructionCancelledEvent>(OnConstructionCancelled);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlacementFailedEvent>(OnPlacementFailed);
            EventBus.Unsubscribe<ConstructionCancelledEvent>(OnConstructionCancelled);
        }

        public void ShowMessage(string message, float duration = -1f)
        {
            if (duration < 0) duration = _defaultDuration;

            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }

            _currentCoroutine = StartCoroutine(ShowCoroutine(message, duration));
        }

        private IEnumerator ShowCoroutine(string message, float duration)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }

            if (_messagePanel != null)
            {
                _messagePanel.SetActive(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;

                float elapsed = 0f;
                while (elapsed < _fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    _canvasGroup.alpha = elapsed / _fadeDuration;
                    yield return null;
                }

                _canvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(duration);

            if (_canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < _fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    _canvasGroup.alpha = 1f - (elapsed / _fadeDuration);
                    yield return null;
                }

                _canvasGroup.alpha = 0f;
            }

            if (_messagePanel != null)
            {
                _messagePanel.SetActive(false);
            }

            _currentCoroutine = null;
        }

        private void HideImmediate()
        {
            if (_messagePanel != null)
            {
                _messagePanel.SetActive(false);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private void OnPlacementFailed(PlacementFailedEvent evt)
        {
            ShowMessage(evt.Message);
        }

        private void OnConstructionCancelled(ConstructionCancelledEvent evt)
        {
            ShowMessage($"건설이 취소되었습니다. 나무 {evt.RefundedWood}개 반환");
        }
    }
}