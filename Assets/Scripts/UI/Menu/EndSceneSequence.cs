using System.Collections;
using TMPro;
using UnityEngine;
using SunnysideIsland.Core;

namespace SunnysideIsland.UI.Menu
{
    public sealed class EndSceneSequence : MonoBehaviour
    {
        [Header("=== Ending Message ===")]
        [SerializeField] private CanvasGroup _endingMessageGroup;
        [SerializeField] private TextMeshProUGUI _endingMessageText;
        [SerializeField] private float _messageHoldTime = 1.5f;
        [SerializeField] private float _messageFadeTime = 1.2f;
        [SerializeField] private string _defaultEndingMessage = "탈출 성공!!!";

        [Header("=== Credits ===")]
        [SerializeField] private GameObject _creditsRoot;
        [SerializeField] private RectTransform _creditsText;
        [SerializeField] private float _creditsScrollSpeed = 35f;
        [SerializeField] private float _creditsEndY = 700f;
        [SerializeField] private float _returnPromptDelay = 1f;

        [Header("=== Return Prompt ===")]
        [SerializeField] private TextMeshProUGUI _returnPromptText;
        [SerializeField] private string _defaultReturnPrompt = "아무 키나 입력";

        private Vector2 _creditsStartPosition;
        private bool _allowReturnOnClick;

        private void Awake()
        {
            if (_endingMessageGroup == null)
            {
                _endingMessageGroup = GetComponentInChildren<CanvasGroup>(true);
            }

            if (_creditsText != null)
            {
                _creditsStartPosition = _creditsText.anchoredPosition;
            }

            if (_creditsRoot != null)
            {
                _creditsRoot.SetActive(false);
            }

            if (_endingMessageText != null && string.IsNullOrWhiteSpace(_endingMessageText.text))
            {
                _endingMessageText.text = _defaultEndingMessage;
            }

            if (_returnPromptText != null)
            {
                _returnPromptText.gameObject.SetActive(false);
                if (string.IsNullOrWhiteSpace(_returnPromptText.text))
                {
                    _returnPromptText.text = _defaultReturnPrompt;
                }
            }
        }

        private void Start()
        {
            StartCoroutine(PlaySequence());
        }

        private void Update()
        {
            if (!_allowReturnOnClick)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
            {
                GameManager.Instance?.ReturnToMainMenu();
            }
        }

        private IEnumerator PlaySequence()
        {
            yield return new WaitForSecondsRealtime(_messageHoldTime);

            yield return FadeMessageOut();

            if (_creditsRoot != null)
            {
                _creditsRoot.SetActive(true);
            }

            if (_creditsText != null)
            {
                _creditsText.anchoredPosition = _creditsStartPosition;
            }

            yield return ScrollCredits();
        }

        private IEnumerator FadeMessageOut()
        {
            if (_endingMessageGroup == null)
            {
                yield break;
            }

            float startAlpha = _endingMessageGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _messageFadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _messageFadeTime);
                _endingMessageGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            _endingMessageGroup.alpha = 0f;
            _endingMessageGroup.gameObject.SetActive(false);
        }

        private IEnumerator ScrollCredits()
        {
            if (_creditsText == null)
            {
                yield break;
            }

            while (_creditsText.anchoredPosition.y < _creditsEndY)
            {
                _creditsText.anchoredPosition += Vector2.up * (_creditsScrollSpeed * Time.unscaledDeltaTime);
                yield return null;
            }

            if (_returnPromptText != null)
            {
                _returnPromptText.gameObject.SetActive(true);
            }

            if (_returnPromptDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(_returnPromptDelay);
            }

            _allowReturnOnClick = true;
        }
    }
}
