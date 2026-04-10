using DI;
using SunnysideIsland.Events;
using SunnysideIsland.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SunnysideIsland.UI.Quest
{
    public class QuestPanel : UIPanel
    {
        [Header("=== Quest List ===")]
        [SerializeField] private Transform _questListContainer;

        [Header("=== Info ===")]
        [SerializeField] private TextMeshProUGUI _questCountText;

        [Header("=== Buttons ===")]
        [SerializeField] private Button _closeButton;

        [Inject]
        private QuestSystem _questSystem;

        private Canvas _questCanvas;
        private bool _questCanvasOverrideSorting;
        private int _questCanvasSortingOrder;
        private TextMeshProUGUI _listText;
        private bool _eventsSubscribed;

        protected override void Awake()
        {
            base.Awake();
            _isModal = true;
            _questCanvas = GetComponentInParent<Canvas>(true);

            if (_questCanvas != null)
            {
                _questCanvasOverrideSorting = _questCanvas.overrideSorting;
                _questCanvasSortingOrder = _questCanvas.sortingOrder;
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            if (_questCanvas != null)
            {
                _questCanvas.overrideSorting = true;
                _questCanvas.sortingOrder = 200;
            }

            SubscribeEvents();
            RefreshQuestList();
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            UnsubscribeEvents();

            if (_questCanvas != null)
            {
                _questCanvas.overrideSorting = _questCanvasOverrideSorting;
                _questCanvas.sortingOrder = _questCanvasSortingOrder;
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }

            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_eventsSubscribed)
            {
                return;
            }

            EventBus.Subscribe<QuestAcceptedEvent>(OnQuestChanged);
            EventBus.Subscribe<QuestCompletedEvent>(OnQuestChanged);
            _eventsSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_eventsSubscribed)
            {
                return;
            }

            EventBus.Unsubscribe<QuestAcceptedEvent>(OnQuestChanged);
            EventBus.Unsubscribe<QuestCompletedEvent>(OnQuestChanged);
            _eventsSubscribed = false;
        }

        private void OnQuestChanged(QuestAcceptedEvent evt)
        {
            RefreshQuestList();
        }

        private void OnQuestChanged(QuestCompletedEvent evt)
        {
            RefreshQuestList();
        }

        private void RefreshQuestList()
        {
            if (_questSystem == null)
            {
                SetListText(string.Empty);
                SetCountText(0);
                return;
            }

            var activeQuests = _questSystem.GetActiveQuests();
            if (activeQuests == null || activeQuests.Count == 0)
            {
                SetListText(string.Empty);
                SetCountText(0);
                return;
            }

            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < activeQuests.Count; i++)
            {
                var quest = activeQuests[i];
                if (quest == null)
                {
                    continue;
                }

                string title = _questSystem.GetQuestTitle(quest.QuestId);
                builder.AppendLine(string.IsNullOrWhiteSpace(title) ? quest.QuestId : title);
            }

            SetListText(builder.ToString().TrimEnd());
            SetCountText(activeQuests.Count);
        }

        private TextMeshProUGUI EnsureListText()
        {
            if (_listText != null)
            {
                return _listText;
            }

            if (_questListContainer == null)
            {
                return null;
            }

            var existing = _questListContainer.GetComponentInChildren<TextMeshProUGUI>(true);
            if (existing != null)
            {
                _listText = existing;
            }

            return _listText;
        }

        private void SetListText(string message)
        {
            var listText = EnsureListText();
            if (listText != null)
            {
                listText.text = message;
            }
        }

        private void SetCountText(int count)
        {
            if (_questCountText != null)
            {
                _questCountText.text = string.Empty;
            }
        }
    }
}
