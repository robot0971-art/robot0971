using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SunnysideIsland.Events;
using System.Collections;

namespace SunnysideIsland.Fishing
{
    public class FishingMiniGame : MonoBehaviour
    {
        public static FishingMiniGame Instance { get; private set; }

        [Header("=== UI References ===")]
        [SerializeField] private GameObject _miniGamePanel;
        [SerializeField] private Slider _fishPositionSlider;
        [SerializeField] private Slider _catchZoneSlider;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private Image _fishIcon;
        [SerializeField] private TMP_Text _instructionText;
        [SerializeField] private TMP_Text _progressText;

        [Header("=== Settings ===")]
        [SerializeField] private float _catchZoneWidth = 0.15f;
        [SerializeField] private float _fishSpeedMin = 0.3f;
        [SerializeField] private float _fishSpeedMax = 0.8f;
        [SerializeField] private float _progressGainRate = 0.3f;
        [SerializeField] private float _progressLossRate = 0.5f;
        [SerializeField] private float _difficultyMultiplier = 1f;
        [SerializeField] private float _maxDuration = 30f;

        private bool _isActive;
        private float _fishPosition;
        private float _fishSpeed;
        private float _fishDirection = 1f;
        private float _catchZoneCenter = 0.5f;
        private float _progress;
        private float _elapsedTime;
        private int _fishDifficulty;
        private Action<bool> _onComplete;

        public bool IsActive => _isActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_miniGamePanel != null)
                _miniGamePanel.SetActive(false);
        }

        public void StartMiniGame(int fishDifficulty, Action<bool> onComplete)
        {
            if (_isActive) return;

            _isActive = true;
            _fishDifficulty = fishDifficulty;
            _onComplete = onComplete;

            _progress = 0f;
            _elapsedTime = 0f;
            _fishPosition = 0.5f;
            _catchZoneCenter = 0.5f;

            float difficultyScale = 1f + (fishDifficulty - 1) * 0.2f;
            _fishSpeed = UnityEngine.Random.Range(_fishSpeedMin, _fishSpeedMax) * difficultyScale * _difficultyMultiplier;

            float zoneWidth = _catchZoneWidth / difficultyScale;
            if (_catchZoneSlider != null)
            {
                _catchZoneSlider.value = zoneWidth;
            }

            SetupUI();

            if (_miniGamePanel != null)
                _miniGamePanel.SetActive(true);

            EventBus.Publish(new FishingMiniGameStartedEvent
            {
                Difficulty = fishDifficulty
            });
        }

        private void SetupUI()
        {
            if (_progressSlider != null)
            {
                _progressSlider.minValue = 0f;
                _progressSlider.maxValue = 1f;
                _progressSlider.value = 0f;
            }

            if (_fishPositionSlider != null)
            {
                _fishPositionSlider.minValue = 0f;
                _fishPositionSlider.maxValue = 1f;
                _fishPositionSlider.value = _fishPosition;
            }

            if (_instructionText != null)
            {
                _instructionText.text = "물고기를 잡으세요!\n[스페이스] 또는 [마우스 클릭]";
            }

            UpdateProgressUI();
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= _maxDuration)
            {
                EndMiniGame(false);
                return;
            }

            UpdateFishMovement();
            HandleInput();
            UpdateProgress();
            UpdateUI();

            if (_progress >= 1f)
            {
                EndMiniGame(true);
            }
            else if (_progress <= 0f && !IsFishInCatchZone())
            {
            }
        }

        private void UpdateFishMovement()
        {
            _fishPosition += _fishSpeed * _fishDirection * Time.deltaTime;

            if (_fishPosition >= 1f)
            {
                _fishPosition = 1f;
                _fishDirection = -1f;
                RandomizeFishSpeed();
            }
            else if (_fishPosition <= 0f)
            {
                _fishPosition = 0f;
                _fishDirection = 1f;
                RandomizeFishSpeed();
            }

            if (UnityEngine.Random.value < 0.02f * _fishDifficulty)
            {
                _fishDirection *= -1f;
                RandomizeFishSpeed();
            }
        }

        private void RandomizeFishSpeed()
        {
            float difficultyScale = 1f + (_fishDifficulty - 1) * 0.2f;
            _fishSpeed = UnityEngine.Random.Range(_fishSpeedMin, _fishSpeedMax) * difficultyScale * _difficultyMultiplier;
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                MoveCatchZone();
            }

            float vertical = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(vertical) > 0.1f)
            {
                _catchZoneCenter += vertical * Time.deltaTime * 0.5f;
                _catchZoneCenter = Mathf.Clamp01(_catchZoneCenter);
            }
        }

        private void MoveCatchZone()
        {
            _catchZoneCenter += 0.15f;
            if (_catchZoneCenter > 1f)
                _catchZoneCenter = 0f;

            if (_catchZoneSlider != null)
            {
                StartCoroutine(PunchScaleRoutine(_catchZoneSlider.transform, Vector3.one * 0.1f, 0.2f));
            }
        }

        private IEnumerator PunchScaleRoutine(Transform target, Vector3 punch, float duration)
        {
            Vector3 originalScale = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Sin(t * Mathf.PI);
                target.localScale = originalScale + punch * scale;
                yield return null;
            }
            target.localScale = originalScale;
        }

        private void UpdateProgress()
        {
            bool inZone = IsFishInCatchZone();

            if (inZone)
            {
                _progress += _progressGainRate * Time.deltaTime;
            }
            else
            {
                _progress -= _progressLossRate * Time.deltaTime;
            }

            _progress = Mathf.Clamp01(_progress);
        }

        private bool IsFishInCatchZone()
        {
            float halfWidth = _catchZoneWidth * 0.5f;
            float minZone = _catchZoneCenter - halfWidth;
            float maxZone = _catchZoneCenter + halfWidth;

            return _fishPosition >= minZone && _fishPosition <= maxZone;
        }

        private void UpdateUI()
        {
            if (_fishPositionSlider != null)
                _fishPositionSlider.value = _fishPosition;

            if (_catchZoneSlider != null)
            {
                RectTransform rect = _catchZoneSlider.GetComponent<RectTransform>();
                if (rect != null)
                {
                    float parentWidth = rect.parent.GetComponent<RectTransform>().rect.width;
                    rect.anchoredPosition = new Vector2((_catchZoneCenter - 0.5f) * parentWidth, rect.anchoredPosition.y);
                }
            }

            UpdateProgressUI();
        }

        private void UpdateProgressUI()
        {
            if (_progressSlider != null)
                _progressSlider.value = _progress;

            if (_progressText != null)
                _progressText.text = $"{Mathf.RoundToInt(_progress * 100)}%";
        }

        private void EndMiniGame(bool success)
        {
            _isActive = false;

            if (_miniGamePanel != null)
            {
                StartCoroutine(ClosePanelRoutine());
            }

            EventBus.Publish(new FishingMiniGameEndedEvent
            {
                Success = success,
                Progress = _progress
            });

            _onComplete?.Invoke(success);
        }

        private IEnumerator ClosePanelRoutine()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 startScale = _miniGamePanel.transform.localScale;
            Vector3 endScale = new Vector3(0.9f, 0.9f, 0.9f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _miniGamePanel.transform.localScale = Vector3.Lerp(startScale, endScale, t * t);
                yield return null;
            }

            _miniGamePanel.SetActive(false);
            _miniGamePanel.transform.localScale = Vector3.one;
        }

        public void CancelMiniGame()
        {
            if (!_isActive) return;
            EndMiniGame(false);
        }
    }

    public class FishingMiniGameStartedEvent
    {
        public int Difficulty { get; set; }
    }

    public class FishingMiniGameEndedEvent
    {
        public bool Success { get; set; }
        public float Progress { get; set; }
    }
}