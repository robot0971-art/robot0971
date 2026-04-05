using System;
using SunnysideIsland.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SunnysideIsland.UI.Components
{
    public class SlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("=== References ===")]
        [SerializeField] protected Image _iconImage;
        [SerializeField] protected Image _backgroundImage;
        [SerializeField] protected Image _selectedOverlay;
        [SerializeField] protected Image _lockOverlay;
        [SerializeField] protected TextMeshProUGUI _quantityText;
        [SerializeField] protected TextMeshProUGUI _keyText;


        [Header("=== Settings ===")]
        [SerializeField] protected bool _showQuantity = true;
        [SerializeField] protected bool _isDraggable = true;
        [SerializeField][Range(0.1f, 1f)] protected float _iconScale = 0.7f; // 아이콘 크기 비율 (85%)


        public int SlotIndex { get; protected set; }
        public string ItemId { get; protected set; }
        public int Quantity { get; protected set; }
        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Quantity <= 0;
        public bool IsSelected { get; protected set; }
        public bool IsLocked { get; protected set; }


        public event Action<SlotUI> OnClicked;
        public event Action<SlotUI> OnRightClicked;
        public event Action<SlotUI> OnDoubleClicked;
        public event Action<SlotUI> OnHoverEnter;
        public event Action<SlotUI> OnHoverExit;
        public event Action<SlotUI, Vector2> OnDragStarted;
        public event Action<SlotUI, Vector2> OnDragging;
        public event Action<SlotUI> OnDragEnded;


        private float _lastClickTime;
        private const float DOUBLE_CLICK_THRESHOLD = 0.3f;


        protected virtual void Awake()
        {
            ResetIconState();
        }


        public virtual void SetItem(string itemId, string itemName, int quantity, Sprite icon = null)
        {
            ItemId = itemId;
            Quantity = quantity;


            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
                _iconImage.preserveAspect = true;

                if (_iconImage.rectTransform != null)
                {
                    var parentRect = _iconImage.rectTransform.parent as RectTransform;
                    if (parentRect != null)
                    {
                        if (parentRect.rect.width == 0 || parentRect.rect.height == 0)
                        {
                            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                        }
                        
                        _iconImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                        _iconImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                        _iconImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                        
                        float parentSize = 64f;
                        if (parentRect.rect.width > 0 && parentRect.rect.height > 0)
                        {
                            parentSize = Mathf.Min(parentRect.rect.width, parentRect.rect.height);
                        }
                        
                        float size = parentSize * GetIconScale(itemId);
                        
                        _iconImage.rectTransform.anchoredPosition = Vector2.zero;
                        _iconImage.rectTransform.sizeDelta = new Vector2(size, size);
                    }
                }
            }


            if (_quantityText != null)
            {
                if (!string.IsNullOrEmpty(itemName))
                {
                    string text = $"{itemName}x{quantity}";
                    _quantityText.text = text;
                    _quantityText.gameObject.SetActive(true);
                    
                    // 자동 폰트 크기 조절 설정
                    _quantityText.enableAutoSizing = true;
                    _quantityText.fontSizeMin = 8;
                    _quantityText.fontSizeMax = 11;
                    _quantityText.fontSize = 11;
                }
                else
                {
                    // itemName이 없으면 quantity만 표시 (1이어도 표시)
                    _quantityText.text = $"x{quantity}";
                    _quantityText.gameObject.SetActive(true);
                    
                    // 자동 폰트 크기 조절 설정
                    _quantityText.enableAutoSizing = true;
                    _quantityText.fontSizeMin = 8;
                    _quantityText.fontSizeMax = 11;
                    _quantityText.fontSize = 11;
                }
            }
            else
            {
                Debug.LogWarning("[SlotUI] _quantityText가 null입니다!");
            }
        }


        public virtual void Clear()
        {
            ItemId = null;
            Quantity = 0;


            ResetIconState();


            if (_quantityText != null)
            {
                _quantityText.text = "";
                _quantityText.gameObject.SetActive(false);
            }

            // 배경색은 유지 (변경하지 않음)
        }


        public void SetSlotIndex(int index)
        {
            SlotIndex = index;
        }


        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (_selectedOverlay != null)
            {
                _selectedOverlay.enabled = selected;
            }
        }


        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            if (_lockOverlay != null)
            {
                _lockOverlay.enabled = locked;
            }
        }


        public void SetKeyText(string text)
        {
            if (_keyText != null)
            {
                _keyText.text = text;
                _keyText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
        }


        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
                _iconImage.preserveAspect = true;
            }
        }


        protected void ResetIconState()
        {
            if (_iconImage == null)
            {
                return;
            }

            _iconImage.sprite = null;
            _iconImage.enabled = false;
            _iconImage.preserveAspect = true;

            if (_iconImage.rectTransform == null)
            {
                return;
            }

            _iconImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _iconImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _iconImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _iconImage.rectTransform.anchoredPosition = Vector2.zero;
            _iconImage.rectTransform.sizeDelta = Vector2.zero;
        }


        protected virtual float GetIconScale(string itemId)
        {
            if (string.Equals(itemId, "pork", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Min(_iconScale, 0.52f);
            }

            return _iconScale;
        }


        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (IsLocked) return;


            float clickTime = Time.time;
            if (clickTime - _lastClickTime < DOUBLE_CLICK_THRESHOLD)
            {
                OnDoubleClicked?.Invoke(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnClicked?.Invoke(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClicked?.Invoke(this);
            }


            _lastClickTime = clickTime;
        }


        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsEmpty)
            {
                OnHoverEnter?.Invoke(this);
            }
        }


        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit?.Invoke(this);
        }


        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isDraggable || IsEmpty || IsLocked) return;
            OnDragStarted?.Invoke(this, eventData.position);
        }


        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDraggable) return;
            OnDragging?.Invoke(this, eventData.position);
        }


        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDraggable) return;
            OnDragEnded?.Invoke(this);
        }
    }
}
