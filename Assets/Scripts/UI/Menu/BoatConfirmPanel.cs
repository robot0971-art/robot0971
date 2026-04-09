using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SunnysideIsland.UI.Menu
{
    public sealed class BoatConfirmPanel : UIPanel
    {
        [Header("=== Canvas ===")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private int _sortingOrder = 500;

        [Header("=== UI ===")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _yesButton;
        [SerializeField] private Button _noButton;

        private Action _onConfirm;

        protected override void Awake()
        {
            base.Awake();
            _isModal = true;

            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            _canvas.overrideSorting = true;
            _canvas.sortingOrder = _sortingOrder;

            ResolveReferences();

            if (_yesButton != null)
            {
                _yesButton.onClick.RemoveListener(HandleYes);
                _yesButton.onClick.AddListener(HandleYes);
            }

            if (_noButton != null)
            {
                _noButton.onClick.RemoveListener(HandleNo);
                _noButton.onClick.AddListener(HandleNo);
            }
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            _onConfirm = null;
        }

        public void Show(Action onConfirm)
        {
            _onConfirm = onConfirm;
            Debug.Log("[BoatConfirmPanel] Show()");
            transform.SetAsLastSibling();

            Open();
        }

        private void ResolveReferences()
        {
            if (_messageText == null)
            {
                _messageText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (_yesButton == null || _noButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                foreach (var button in buttons)
                {
                    if (button == null)
                    {
                        continue;
                    }

                    string buttonName = button.gameObject.name.ToLowerInvariant();
                    if (_yesButton == null && buttonName.Contains("yes"))
                    {
                        _yesButton = button;
                    }
                    else if (_noButton == null && buttonName.Contains("no"))
                    {
                        _noButton = button;
                    }
                }
            }
        }

        private void HandleYes()
        {
            Debug.Log("[BoatConfirmPanel] Yes clicked");
            var confirm = _onConfirm;
            _onConfirm = null;
            confirm?.Invoke();
            Close();
        }

        private void HandleNo()
        {
            Debug.Log("[BoatConfirmPanel] No clicked");
            Close();
            _onConfirm = null;
        }
    }
}
