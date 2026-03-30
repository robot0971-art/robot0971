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
        [SerializeField] private Sprite _hammerSprite;

        private Building _building;

        private void Awake()
        {
            _building = GetComponentInParent<Building>();

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
            }
        }

        private void Update()
        {
            if (_building == null) return;

            UpdateVisibility();
            UpdateProgress();

            if (_progressPanel != null && _progressPanel.activeSelf)
            {
                _progressPanel.transform.position = _building.transform.position + _offset;
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