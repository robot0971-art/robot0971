using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SunnysideIsland.UI.Components
{
    public class NotificationArea : MonoBehaviour
    {
        [Header("=== Settings ===")]
        [SerializeField] private int _maxNotifications = 5;
        [SerializeField] private float _defaultDuration = 3f;
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private float _spacing = 5f;
        
        [Header("=== Prefab ===")]
        [SerializeField] private GameObject _notificationPrefab;
        
        [Header("=== Animation ===")]
        [SerializeField] private bool _useAnimation = true;
        [SerializeField] private float _slideInDuration = 0.3f;
        [SerializeField] private AnimationCurve _slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private readonly Queue<NotificationItem> _notificationQueue = new Queue<NotificationItem>();
        private readonly List<GameObject> _activeNotifications = new List<GameObject>();
        private RectTransform _rectTransform;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }
        
        public void ShowNotification(string message, float duration = -1f)
        {
            if (duration < 0) duration = _defaultDuration;
            
            var item = new NotificationItem
            {
                Message = message,
                Duration = duration
            };
            
            _notificationQueue.Enqueue(item);
            
            if (_activeNotifications.Count < _maxNotifications)
            {
                ShowNextNotification();
            }
        }
        
        public void ShowNotification(string message, Color color, float duration = -1f)
        {
            if (duration < 0) duration = _defaultDuration;
            
            var item = new NotificationItem
            {
                Message = message,
                Duration = duration,
                Color = color
            };
            
            _notificationQueue.Enqueue(item);
            
            if (_activeNotifications.Count < _maxNotifications)
            {
                ShowNextNotification();
            }
        }
        
        private void ShowNextNotification()
        {
            if (_notificationQueue.Count == 0) return;
            
            var item = _notificationQueue.Dequeue();
            var notificationGO = CreateNotification(item);
            
            if (notificationGO != null)
            {
                _activeNotifications.Insert(0, notificationGO);
                StartCoroutine(AnimateNotification(notificationGO, item.Duration));
            }
        }
        
        private GameObject CreateNotification(NotificationItem item)
        {
            GameObject notificationGO;
            
            if (_notificationPrefab != null)
            {
                notificationGO = Instantiate(_notificationPrefab, transform);
            }
            else
            {
                notificationGO = CreateDefaultNotification();
            }
            
            var textComponent = notificationGO.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = item.Message;
                if (item.Color.HasValue)
                {
                    textComponent.color = item.Color.Value;
                }
            }
            
            return notificationGO;
        }
        
        private GameObject CreateDefaultNotification()
        {
            var go = new GameObject("Notification");
            go.transform.SetParent(transform, false);
            
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(0, 30);
            
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;
            
            var canvasGroup = go.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            
            return go;
        }
        
        private IEnumerator AnimateNotification(GameObject notification, float duration)
        {
            var rectTransform = notification.GetComponent<RectTransform>();
            var canvasGroup = notification.GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = notification.AddComponent<CanvasGroup>();
            }
            
            if (_useAnimation)
            {
                Vector2 startPos = rectTransform.anchoredPosition;
                startPos.y += 50;
                Vector2 endPos = rectTransform.anchoredPosition;
                
                float elapsed = 0f;
                while (elapsed < _slideInDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / _slideInDuration;
                    float curveValue = _slideInCurve.Evaluate(t);
                    
                    rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);
                    canvasGroup.alpha = t;
                    
                    yield return null;
                }
                
                rectTransform.anchoredPosition = endPos;
                canvasGroup.alpha = 1f;
            }
            else
            {
                canvasGroup.alpha = 1f;
            }
            
            yield return new WaitForSeconds(duration);
            
            float fadeElapsed = 0f;
            while (fadeElapsed < _fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (fadeElapsed / _fadeDuration);
                yield return null;
            }
            
            _activeNotifications.Remove(notification);
            Destroy(notification);
            
            if (_notificationQueue.Count > 0)
            {
                ShowNextNotification();
            }
        }
        
        public void ClearAll()
        {
            StopAllCoroutines();
            
            foreach (var notification in _activeNotifications)
            {
                if (notification != null)
                {
                    Destroy(notification);
                }
            }
            
            _activeNotifications.Clear();
            _notificationQueue.Clear();
        }
        
        private class NotificationItem
        {
            public string Message;
            public float Duration;
            public Color? Color;
        }
    }
}