using System;
using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.Quest
{
    /// <summary>
    /// 퀘스트 상태
    /// </summary>
    public enum QuestState
    {
        Inactive,
        Active,
        Completed,
        Rewarded
    }

    /// <summary>
    /// 퀘스트 데이터
    /// </summary>
    [Serializable]
    public class QuestData
    {
        public string QuestId;
        public string Title;
        public string Description;
        public string[] Objectives;
        public int[] Rewards;
        public string NextQuestId;
    }

    /// <summary>
    /// 퀘스트
    /// </summary>
    [Serializable]
    public class Quest
    {
        public string QuestId;
        public QuestState State;
        public int[] Progress;
        
        public bool IsCompleted => State == QuestState.Completed || State == QuestState.Rewarded;
    }

    /// <summary>
    /// 퀘스트 시스템
    /// </summary>
    public class QuestSystem : MonoBehaviour, ISaveable
    {
        [Header("=== Quest Data ===")]
        [SerializeField] private List<QuestData> _questDatabase = new List<QuestData>();
        
        private List<Quest> _activeQuests = new List<Quest>();
        private List<string> _completedQuests = new List<string>();
        
        public string SaveKey => "QuestSystem";
        
        public void AcceptQuest(string questId)
        {
            if (HasQuest(questId)) return;
            
            var questData = FindQuestData(questId);
            if (questData == null) return;
            
            var quest = new Quest
            {
                QuestId = questId,
                State = QuestState.Active,
                Progress = new int[questData.Objectives.Length]
            };
            
            _activeQuests.Add(quest);
            
            EventBus.Publish(new QuestAcceptedEvent
            {
                QuestId = questId
            });
        }
        
        public void UpdateProgress(string questId, int objectiveIndex, int amount)
        {
            var quest = FindQuest(questId);
            if (quest == null) return;
            if (quest.State != QuestState.Active) return;
            
            var questData = FindQuestData(questId);
            if (questData == null) return;
            
            quest.Progress[objectiveIndex] += amount;
            
            // 목표 완료 체크
            bool allCompleted = true;
            for (int i = 0; i < quest.Progress.Length; i++)
            {
                if (quest.Progress[i] < GetObjectiveTarget(questData.Objectives[i]))
                {
                    allCompleted = false;
                    break;
                }
            }
            
            if (allCompleted)
            {
                CompleteQuest(questId);
            }
        }
        
        public void CompleteQuest(string questId)
        {
            var quest = FindQuest(questId);
            if (quest == null) return;
            
            quest.State = QuestState.Completed;
            
            EventBus.Publish(new QuestCompletedEvent
            {
                QuestId = questId
            });
        }
        
        public void ClaimReward(string questId)
        {
            var quest = FindQuest(questId);
            if (quest == null) return;
            if (quest.State != QuestState.Completed) return;
            
            var questData = FindQuestData(questId);
            if (questData == null) return;
            
            // 보상 지급
            quest.State = QuestState.Rewarded;
            _completedQuests.Add(questId);
            _activeQuests.Remove(quest);
            
            // 다음 퀘스트 자동 수락
            if (!string.IsNullOrEmpty(questData.NextQuestId))
            {
                AcceptQuest(questData.NextQuestId);
            }
        }
        
        public bool HasQuest(string questId)
        {
            return FindQuest(questId) != null;
        }
        
        public bool IsQuestCompleted(string questId)
        {
            var quest = FindQuest(questId);
            return quest?.IsCompleted ?? false;
        }
        
        private Quest FindQuest(string questId)
        {
            foreach (var quest in _activeQuests)
            {
                if (quest.QuestId == questId)
                    return quest;
            }
            return null;
        }
        
        private QuestData FindQuestData(string questId)
        {
            foreach (var data in _questDatabase)
            {
                if (data.QuestId == questId)
                    return data;
            }
            return null;
        }
        
        private int GetObjectiveTarget(string objective)
        {
            // "Collect_Wood_10" 형식 파싱
            var parts = objective.Split('_');
            if (parts.Length >= 3)
            {
                return int.Parse(parts[parts.Length - 1]);
            }
            return 1;
        }
        
        public object GetSaveData()
        {
            return new QuestSaveData
            {
                ActiveQuests = _activeQuests,
                CompletedQuests = _completedQuests
            };
        }
        
        public void LoadSaveData(object state)
        {
            if (state is QuestSaveData data)
            {
                _activeQuests = data.ActiveQuests ?? new List<Quest>();
                _completedQuests = data.CompletedQuests ?? new List<string>();
            }
        }
    }
    
    [Serializable]
    public class QuestSaveData
    {
        public List<Quest> ActiveQuests;
        public List<string> CompletedQuests;
    }
    
    /// <summary>
    /// 퀘스트 수락 이벤트
    /// </summary>
    public class QuestAcceptedEvent
    {
        public string QuestId { get; set; }
    }
    
    /// <summary>
    /// 퀘스트 완료 이벤트
    /// </summary>
    public class QuestCompletedEvent
    {
        public string QuestId { get; set; }
    }
}
