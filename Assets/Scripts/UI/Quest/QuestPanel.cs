using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.Events;
using SunnysideIsland.Quest;
using SunnysideIsland.GameData;

namespace SunnysideIsland.UI.Quest
{
    public class QuestPanel : UIPanel
    {
        [Header("=== Quest List ===")]
        [SerializeField] private Transform _questListContainer;
        [SerializeField] private GameObject _questItemPrefab;
        
        [Header("=== Tabs ===")]
        [SerializeField] private Button _activeTabButton;
        [SerializeField] private Button _completedTabButton;
        [SerializeField] private Color _selectedTabColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color _unselectedTabColor = new Color(0.5f, 0.5f, 0.5f);
        
        [Header("=== Info ===")]
        [SerializeField] private TextMeshProUGUI _questCountText;
        [SerializeField] private QuestDetailPanel _questDetailPanel;
        
        [Header("=== Buttons ===")]
        [SerializeField] private Button _closeButton;
        
        [Inject]
        private QuestSystem _questSystem;
        [Inject]
        private QuestDatabase _questDatabase;
        
        private enum QuestTab { Active, Completed }
        private QuestTab _currentTab = QuestTab.Active;
        private string _selectedQuestId;
        private readonly List<GameObject> _questItems = new List<GameObject>();
        
        protected override void Awake()
        {
            base.Awake();
            _isModal = true;
            
            _activeTabButton?.onClick.AddListener(() => SwitchTab(QuestTab.Active));
            _completedTabButton?.onClick.AddListener(() => SwitchTab(QuestTab.Completed));
            _closeButton?.onClick.AddListener(Close);
        }
        
        protected override void OnOpened()
        {
            base.OnOpened();
            SwitchTab(QuestTab.Active);
            SubscribeEvents();
        }
        
        protected override void OnClosed()
        {
            base.OnClosed();
            UnsubscribeEvents();
            ClearQuestList();
            _questDetailPanel?.Hide();
        }
        
        private void SubscribeEvents()
        {
            EventBus.Subscribe<QuestAcceptedEvent>(OnQuestAccepted);
            EventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
        }
        
        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<QuestAcceptedEvent>(OnQuestAccepted);
            EventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
        }
        
        private void SwitchTab(QuestTab tab)
        {
            _currentTab = tab;
            _selectedQuestId = null;
            
            UpdateTabColors();
            RefreshQuestList();
            _questDetailPanel?.Hide();
        }
        
        private void UpdateTabColors()
        {
            if (_activeTabButton != null)
            {
                var colors = _activeTabButton.colors;
                colors.normalColor = _currentTab == QuestTab.Active ? _selectedTabColor : _unselectedTabColor;
                _activeTabButton.colors = colors;
            }
            
            if (_completedTabButton != null)
            {
                var colors = _completedTabButton.colors;
                colors.normalColor = _currentTab == QuestTab.Completed ? _selectedTabColor : _unselectedTabColor;
                _completedTabButton.colors = colors;
            }
        }
        
        private void RefreshQuestList()
        {
            ClearQuestList();
            
            if (_questSystem == null) return;
            
            var quests = _currentTab == QuestTab.Active 
                ? GetActiveQuests() 
                : GetCompletedQuests();
            
            foreach (var quest in quests)
            {
                CreateQuestItem(quest);
            }
            
            UpdateQuestCount();
        }
        
        private List<DetailedQuestData> GetActiveQuests()
        {
            var result = new List<DetailedQuestData>();
            
            if (_questDatabase == null) return result;
            
            var allQuests = _questDatabase.GetAllQuests();
            foreach (var questData in allQuests)
            {
                if (_questSystem.HasQuest(questData.QuestId) && !_questSystem.IsQuestCompleted(questData.QuestId))
                {
                    result.Add(questData);
                }
            }
            
            return result;
        }
        
        private List<DetailedQuestData> GetCompletedQuests()
        {
            var result = new List<DetailedQuestData>();
            
            if (_questDatabase == null) return result;
            
            var allQuests = _questDatabase.GetAllQuests();
            foreach (var questData in allQuests)
            {
                if (_questSystem.IsQuestCompleted(questData.QuestId))
                {
                    result.Add(questData);
                }
            }
            
            return result;
        }
        
        private void CreateQuestItem(DetailedQuestData questData)
        {
            if (_questItemPrefab == null || _questListContainer == null) return;
            
            var itemGO = Instantiate(_questItemPrefab, _questListContainer);
            
            var questNameText = itemGO.transform.Find("QuestName")?.GetComponent<TextMeshProUGUI>();
            if (questNameText != null)
            {
                questNameText.text = questData.Title;
            }
            
            var chapterText = itemGO.transform.Find("Chapter")?.GetComponent<TextMeshProUGUI>();
            if (chapterText != null)
            {
                chapterText.text = GetChapterDisplayName(questData.Chapter);
            }
            
            var mainQuestIndicator = itemGO.transform.Find("MainQuestIndicator")?.gameObject;
            if (mainQuestIndicator != null)
            {
                mainQuestIndicator.SetActive(questData.IsMainQuest);
            }
            
            var button = itemGO.GetComponent<Button>();
            if (button != null)
            {
                string questId = questData.QuestId;
                button.onClick.AddListener(() => OnQuestItemClicked(questId));
            }
            
            if (!string.IsNullOrEmpty(_selectedQuestId) && _selectedQuestId == questData.QuestId)
            {
                var selectedImage = itemGO.transform.Find("Selected")?.GetComponent<Image>();
                if (selectedImage != null)
                {
                    selectedImage.enabled = true;
                }
            }
            
            _questItems.Add(itemGO);
        }
        
        private string GetChapterDisplayName(ChapterType chapter)
        {
            return chapter switch
            {
                ChapterType.Chapter1 => "챕터 1",
                ChapterType.Chapter2 => "챕터 2",
                ChapterType.Chapter3 => "챕터 3",
                ChapterType.Chapter4 => "챕터 4",
                _ => chapter.ToString()
            };
        }
        
        private void ClearQuestList()
        {
            foreach (var item in _questItems)
            {
                if (item != null) Destroy(item);
            }
            _questItems.Clear();
        }
        
        private void UpdateQuestCount()
        {
            if (_questCountText != null)
            {
                _questCountText.text = $"({_questItems.Count})";
            }
        }
        
        private void OnQuestItemClicked(string questId)
        {
            _selectedQuestId = questId;
            
            if (_questDatabase == null || _questDetailPanel == null) return;
            
            var questData = _questDatabase.GetQuest(questId);
            if (questData != null)
            {
                var quest = GetQuestProgress(questId);
                _questDetailPanel.ShowQuest(questData, quest);
            }
        }
        
        private SunnysideIsland.Quest.Quest GetQuestProgress(string questId)
        {
            return null;
        }
        
        private void OnQuestAccepted(QuestAcceptedEvent evt)
        {
            if (_currentTab == QuestTab.Active)
            {
                RefreshQuestList();
            }
        }
        
        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            RefreshQuestList();
        }
    }
}