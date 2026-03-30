using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SunnysideIsland.UI.Components
{
    public class StatBar : MonoBehaviour
    {
        [Header("=== References ===")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private TextMeshProUGUI _maxValueText;
        
        [Header("=== Settings ===")]
        [SerializeField] private bool _showValueText = true;
        [SerializeField] private bool _showMaxValue = true;
        [SerializeField] private bool _smoothFill = true;
        [SerializeField] private float _smoothSpeed = 5f;
        
        [Header("=== Colors ===")]
        [SerializeField] private Color _normalColor = Color.green;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _criticalColor = Color.red;
        [SerializeField] private float _warningThreshold = 0.5f;
        [SerializeField] private float _criticalThreshold = 0.25f;
        
        private float _currentValue;
        private float _maxValue;
        private float _displayFillAmount;
        private float _targetFillAmount;
        
        public float CurrentValue => _currentValue;
        public float MaxValue => _maxValue;
        
        private void Awake()
        {
            if (_fillImage == null)
            {
                _fillImage = transform.Find("Fill")?.GetComponent<Image>();
            }
        }
        
        private void Update()
        {
            if (_smoothFill && _fillImage != null)
            {
                if (Mathf.Abs(_displayFillAmount - _targetFillAmount) > 0.001f)
                {
                    _displayFillAmount = Mathf.Lerp(_displayFillAmount, _targetFillAmount, Time.deltaTime * _smoothSpeed);
                    _fillImage.fillAmount = _displayFillAmount;
                }
            }
        }
        
        public void SetValue(float current, float max)
        {
            _currentValue = current;
            _maxValue = max;
            
            if (max <= 0)
            {
                _targetFillAmount = 0f;
                _displayFillAmount = 0f;
            }
            else
            {
                _targetFillAmount = Mathf.Clamp01(current / max);
            }
            
            if (!_smoothFill && _fillImage != null)
            {
                _fillImage.fillAmount = _targetFillAmount;
                _displayFillAmount = _targetFillAmount;
            }
            
            UpdateColor();
            UpdateText();
        }
        
        public void SetColor(Color color)
        {
            if (_fillImage != null)
            {
                _fillImage.color = color;
            }
        }
        
        public void SetColors(Color normal, Color warning, Color critical)
        {
            _normalColor = normal;
            _warningColor = warning;
            _criticalColor = critical;
            UpdateColor();
        }
        
        public void SetThresholds(float warning, float critical)
        {
            _warningThreshold = warning;
            _criticalThreshold = critical;
            UpdateColor();
        }
        
        private void UpdateColor()
        {
            if (_fillImage == null) return;
            
            if (_targetFillAmount <= _criticalThreshold)
            {
                _fillImage.color = _criticalColor;
            }
            else if (_targetFillAmount <= _warningThreshold)
            {
                _fillImage.color = _warningColor;
            }
            else
            {
                _fillImage.color = _normalColor;
            }
        }
        
        private void UpdateText()
        {
            if (!_showValueText) return;
            
            if (_valueText != null)
            {
                _valueText.text = Mathf.RoundToInt(_currentValue).ToString();
            }
            
            if (_maxValueText != null)
            {
                if (_showMaxValue)
                {
                    _maxValueText.text = $"/ {Mathf.RoundToInt(_maxValue)}";
                }
                else
                {
                    _maxValueText.gameObject.SetActive(false);
                }
            }
        }
        
        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }
    }
}