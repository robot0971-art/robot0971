using UnityEngine;

namespace SunnysideIsland.Building
{
    public class BuildingPreview : MonoBehaviour
    {
        [Header("=== Visual Settings ===")]
        [SerializeField] private Color _validColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0f, 0f, 0.5f);
        
        private SpriteRenderer _spriteRenderer;
        private bool _isValid;
        
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
        
        public void SetSprite(Sprite sprite)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }
        
        public void SetValid(bool isValid)
        {
            _isValid = isValid;
            UpdateColor();
        }
        
        public void SetSize(BuildingSize size)
        {
            transform.localScale = new Vector3(size.Width, size.Height, 1f);
        }
        
        private void UpdateColor()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _isValid ? _validColor : _invalidColor;
            }
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}