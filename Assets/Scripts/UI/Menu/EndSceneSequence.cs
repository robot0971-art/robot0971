using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SunnysideIsland.UI.Menu
{
    public sealed class EndSceneSequence : MonoBehaviour
    {
        [Header("=== Ending Message ===")]
        [SerializeField] private CanvasGroup _endingMessageGroup;
        [SerializeField] private float _messageHoldTime = 1.5f;
        [SerializeField] private float _messageFadeTime = 1.2f;

        [Header("=== Credits ===")]
        [SerializeField] private GameObject _creditsRoot;
        [SerializeField] private RectTransform _creditsText;
        [SerializeField] private float _creditsScrollSpeed = 35f;
        [SerializeField] private float _creditsEndY = 700f;

        [Header("=== Optional ===")]
        [SerializeField] private Button _mainMenuButton;

        private Vector2 _creditsStartPosition;

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

            if (_mainMenuButton != null)
            {
                _mainMenuButton.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            StartCoroutine(PlaySequence());
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

            if (_mainMenuButton != null)
            {
                _mainMenuButton.gameObject.SetActive(true);
            }
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
        }
    }
}
