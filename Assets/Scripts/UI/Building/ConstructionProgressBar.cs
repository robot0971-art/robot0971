using SunnysideIsland.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SunnysideIsland.Building
{
    public class ConstructionProgressBar : MonoBehaviour
    {
        [Header("=== UI References ===")]
        [SerializeField] private GameObject _progressPanel;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private Image _hammerIcon;

        [Header("=== Settings ===")]
        [SerializeField] private Vector3 _offset = new Vector3(0, -1f, 0);
        [SerializeField] private float _progressBarScale = 1.0f;
        [SerializeField] private Sprite _hammerSprite;

        private Building _building;
        private Vector3 _initialLocalScale;

        private void Awake()
        {
            _building = GetComponentInParent<Building>();
            _initialLocalScale = transform.localScale;

            if (_progressPanel != null)
            {
                _progressPanel.SetActive(false);
            }

            if (_hammerSprite != null && _hammerIcon != null)
            {
                _hammerIcon.sprite = _hammerSprite;
            }
        }

        private void Start()
        {
            if (_building != null)
            {
                UpdateVisibility();
                UpdateProgress();
                UpdateTransform();
            }
        }

        private void Update()
        {
            if (_building == null) return;

            UpdateVisibility();
            UpdateProgress();
            UpdateTransform();
        }

        private void UpdateTransform()
        {
            if (_building == null) return;

            // 건물에 설정된 오프셋 적용
            transform.localPosition = _building.ProgressBarOffset;

            // 부모의 월드 스케일을 역으로 계산하여 일정한 크기 유지 + 건물에 설정된 배율 적용
            if (transform.parent != null)
            {
                Vector3 parentScale = transform.parent.lossyScale;
                float customScale = _building.ProgressBarScale;

                if (parentScale.x != 0 && parentScale.y != 0 && parentScale.z != 0)
                {
                    transform.localScale = new Vector3(
                        (_initialLocalScale.x * customScale) / parentScale.x,
                        (_initialLocalScale.y * customScale) / parentScale.y,
                        (_initialLocalScale.z * customScale) / parentScale.z
                    );
                }
            }
        }

        private void UpdateVisibility()
        {
            if (_building == null || _progressPanel == null) return;

            bool shouldShow = _building.State == BuildingState.Constructing;

            if (_progressPanel.activeSelf != shouldShow)
            {
                _progressPanel.SetActive(shouldShow);
            }
        }

        private void UpdateProgress()
        {
            if (_building == null || _progressSlider == null) return;

            if (_building.State == BuildingState.Constructing)
            {
                float progress = (float)_building.ConstructionProgress / _building.RequiredConstructionDays;
                _progressSlider.value = progress * _progressSlider.maxValue;
            }
        }

        public void SetHammerSprite(Sprite sprite)
        {
            _hammerSprite = sprite;
            if (_hammerIcon != null)
            {
                _hammerIcon.sprite = sprite;
            }
        }
    }
}