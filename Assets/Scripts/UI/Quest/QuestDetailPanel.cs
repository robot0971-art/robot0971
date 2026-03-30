using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SunnysideIsland.Quest;
using SunnysideIsland.GameData;
using SunnysideIsland.Events;

namespace SunnysideIsland.UI.Quest
{
    public class QuestDetailPanel : MonoBehaviour
    {
        [Header("=== Quest Info ===")]
        [SerializeField] private TextMeshProUGUI _questTitleText;
        [SerializeField] private TextMeshProUGUI _questDescriptionText;
        [SerializeField] private TextMeshProUGUI _chapterText;
        [SerializeField] private GameObject _mainQuestIndicator;
        
        [Header("=== Objectives ===")]
        [SerializeField] private Transform _objectiveContainer;
        [SerializeField] private GameObject _objectiveItemPrefab;
        
        [Header("=== Rewards ===")]
        [SerializeField] private Transform _rewardContainer;
        [SerializeField] private GameObject _rewardItemPrefab;
        
        [Header("=== Buttons ===")]
        [SerializeField] private Button _claimRewardButton;
        [SerializeField] private Button _abandonButton;
        [SerializeField] private GameObject _completedIndicator;
        
        private string _currentQuestId;
        private SunnysideIsland.Quest.Quest _currentQuest;
        private readonly List<GameObject> _objectiveItems = new List<GameObject>();
        private readonly List<GameObject> _rewardItems = new List<GameObject>();
        
        private void Awake()
        {
            _claimRewardButton?.onClick.AddListener(OnClaimRewardClicked);
            _abandonButton?.onClick.AddListener(OnAbandonClicked);
            Hide();
        }
        
        public void ShowQuest(DetailedQuestData questData, SunnysideIsland.Quest.Quest questProgress)
        {
            if (questData == null) return;
            
            _currentQuestId = questData.QuestId;
            _currentQuest = questProgress;
            
            gameObject.SetActive(true);
            
            UpdateQuestInfo(questData);
            UpdateObjectives(questData, questProgress);
            UpdateRewards(questData.Reward);
            UpdateButtons(questProgress);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            ClearObjectives();
            ClearRewards();
        }
        
        private void UpdateQuestInfo(DetailedQuestData questData)
        {
            if (_questTitleText != null)
            {
                _questTitleText.text = questData.Title;
            }
            
            if (_questDescriptionText != null)
            {
                _questDescriptionText.text = questData.Description;
            }
            
            if (_chapterText != null)
            {
                _chapterText.text = GetChapterDisplayName(questData.Chapter);
            }
            
            if (_mainQuestIndicator != null)
            {
                _mainQuestIndicator.SetActive(questData.IsMainQuest);
            }
        }
        
        private string GetChapterDisplayName(ChapterType chapter)
        {
            return chapter switch
            {
                ChapterType.Chapter1 => "챕터 1: 생존의 시작",
                ChapterType.Chapter2 => "챕터 2: 섬 개척",
                ChapterType.Chapter3 => "챕터 3: 마을 건설",
                ChapterType.Chapter4 => "챕터 4: 관광 도시",
                _ => chapter.ToString()
            };
        }
        
        private void UpdateObjectives(DetailedQuestData questData, SunnysideIsland.Quest.Quest questProgress)
        {
            ClearObjectives();
            
            if (questData.Objectives == null || _objectiveContainer == null) return;
            
            for (int i = 0; i < questData.Objectives.Length; i++)
            {
                var objective = questData.Objectives[i];
                CreateObjectiveItem(objective, i, questProgress);
            }
        }
        
        private void CreateObjectiveItem(QuestObjective objective, int index, SunnysideIsland.Quest.Quest questProgress)
        {
            if (_objectiveItemPrefab == null || _objectiveContainer == null) return;
            
            var itemGO = Instantiate(_objectiveItemPrefab, _objectiveContainer);
            
            var objectiveText = itemGO.transform.Find("ObjectiveText")?.GetComponent<TextMeshProUGUI>();
            if (objectiveText != null)
            {
                objectiveText.text = objective.Description;
            }
            
            var progressText = itemGO.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
            if (progressText != null)
            {
                int current = questProgress?.Progress != null && index < questProgress.Progress.Length 
                    ? questProgress.Progress[index] 
                    : 0;
                progressText.text = $"{current} / {objective.RequiredAmount}";
            }
            
            var checkmark = itemGO.transform.Find("Checkmark")?.gameObject;
            if (checkmark != null && questProgress?.Progress != null && index < questProgress.Progress.Length)
            {
                checkmark.SetActive(questProgress.Progress[index] >= objective.RequiredAmount);
            }
            
            _objectiveItems.Add(itemGO);
        }
        
        private void ClearObjectives()
        {
            foreach (var item in _objectiveItems)
            {
                if (item != null) Destroy(item);
            }
            _objectiveItems.Clear();
        }
        
        private void UpdateRewards(QuestReward reward)
        {
            ClearRewards();
            
            if (reward == null || _rewardContainer == null) return;
            
            if (reward.Gold > 0)
            {
                CreateRewardItem("골드", $"{reward.Gold:N0} G");
            }
            
            if (reward.Experience > 0)
            {
                CreateRewardItem("경험치", $"{reward.Experience}");
            }
            
            if (reward.ItemIds != null && reward.ItemAmounts != null)
            {
                for (int i = 0; i < reward.ItemIds.Count && i < reward.ItemAmounts.Count; i++)
                {
                    CreateRewardItem(reward.ItemIds[i], $"x{reward.ItemAmounts[i]}");
                }
            }
        }
        
        private void CreateRewardItem(string name, string amount)
        {
            if (_rewardItemPrefab == null || _rewardContainer == null) return;
            
            var itemGO = Instantiate(_rewardItemPrefab, _rewardContainer);
            
            var nameText = itemGO.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = name;
            }
            
            var amountText = itemGO.transform.Find("Amount")?.GetComponent<TextMeshProUGUI>();
            if (amountText != null)
            {
                amountText.text = amount;
            }
            
            _rewardItems.Add(itemGO);
        }
        
        private void ClearRewards()
        {
            foreach (var item in _rewardItems)
            {
                if (item != null) Destroy(item);
            }
            _rewardItems.Clear();
        }
        
        private void UpdateButtons(SunnysideIsland.Quest.Quest questProgress)
        {
            bool isCompleted = questProgress?.State == QuestState.Completed;
            
            if (_claimRewardButton != null)
            {
                _claimRewardButton.gameObject.SetActive(isCompleted);
            }
            
            if (_abandonButton != null)
            {
                _abandonButton.gameObject.SetActive(!isCompleted && questProgress != null);
            }
            
            if (_completedIndicator != null)
            {
                _completedIndicator.SetActive(questProgress?.IsCompleted ?? false);
            }
        }
        
        private void OnClaimRewardClicked()
        {
            if (string.IsNullOrEmpty(_currentQuestId)) return;
            
            var questSystem = FindObjectOfType<QuestSystem>();
            if (questSystem != null)
            {
                questSystem.ClaimReward(_currentQuestId);
            }
            
            Hide();
        }
        
        private void OnAbandonClicked()
        {
            Hide();
        }
    }
}