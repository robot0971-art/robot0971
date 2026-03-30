using UnityEngine;

namespace SunnysideIsland.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        [Header("=== Panel Settings ===")]
        [SerializeField] protected bool _isModal = false;
        [SerializeField] protected bool _pauseGameWhenOpen = false;
        [SerializeField] protected bool _closeOnEscape = true;
        
        protected CanvasGroup _canvasGroup;
        protected bool _isOpen = false;
        
        public bool IsOpen => _isOpen;
        public bool IsModal => _isModal;
        
        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        public virtual void Open()
        {
            if (_isOpen) return;
            
            _isOpen = true;
            gameObject.SetActive(true);
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            
            if (_pauseGameWhenOpen)
            {
                Time.timeScale = 0f;
            }
            
            OnOpened();
        }
        
        public virtual void Close()
        {
            if (!_isOpen) return;
            
            _isOpen = false;
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            
            if (_pauseGameWhenOpen)
            {
                Time.timeScale = 1f;
            }
            
            OnClosed();
            gameObject.SetActive(false);
        }
        
        public virtual void Toggle()
        {
            if (_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }
        
        public virtual void OnBackButton()
        {
            if (_closeOnEscape)
            {
                Close();
            }
        }
        
        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }
    }
}