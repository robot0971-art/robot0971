using System;
using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.Quest
{
    /// <summary>
    /// 챕터 타입
    /// </summary>
    public enum ChapterType
    {
        Chapter1, // Day 1-3: 생존의 시작
        Chapter2, // Day 4-7: 섬 개척
        Chapter3, // Day 8-14: 마을 건설
        Chapter4  // Day 15-28: 관광 도시 완성
    }

    /// <summary>
    /// 챕터 데이터
    /// </summary>
    [Serializable]
    public class ChapterData
    {
        public ChapterType Chapter;
        public string Title;
        public string Description;
        public int StartDay;
        public int EndDay;
        public string[] QuestIds;
        public string[] SubQuestIds;
        public bool IsCompleted;
    }

    /// <summary>
    /// 챕터 퀘스트 관리자
    /// 모든 메인 퀘스트와 챕터 진행을 관리
    /// </summary>
    public class ChapterQuestManager : MonoBehaviour, ISaveable
    {
        [Header("=== Chapter Data ===")]
        [SerializeField] private List<ChapterData> _chapters = new List<ChapterData>();
        
        [Header("=== Current State ===")]
        [SerializeField] private ChapterType _currentChapter = ChapterType.Chapter1;
        [SerializeField] private int _currentDay = 1;
        
        private QuestSystem _questSystem;
        private TimeManager _timeManager;
        
        public string SaveKey => "ChapterQuestManager";
        public ChapterType CurrentChapter => _currentChapter;
        public int CurrentDay => _currentDay;
        
        public event Action<ChapterType> OnChapterStarted;
        public event Action<ChapterType> OnChapterCompleted;
        
        private void Awake()
        {
        }
        
        private void Start()
        {
            _questSystem = DI.DIContainer.Resolve<QuestSystem>();
            _timeManager = DI.DIContainer.Resolve<TimeManager>();
            
            EventBus.Subscribe<DayStartedEvent>(OnDayStartedEvent);
            EventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
        }
        
        private void OnDestroy()
        {
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStartedEvent);
            EventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
        }
        
        private void OnDayStartedEvent(DayStartedEvent evt)
        {
            _currentDay = evt.Day;
            
            // 새로운 챕터 시작 체크
            foreach (var chapter in _chapters)
            {
                if (chapter.StartDay == evt.Day && chapter.Chapter > _currentChapter)
                {
                    StartChapter(chapter.Chapter);
                    break;
                }
            }
            
            // 일일 퀘스트 생성
            GenerateDailyQuests();
        }
        
        /// <summary>
        /// 챕터 시작
        /// </summary>
        public void StartChapter(ChapterType chapter)
        {
            _currentChapter = chapter;
            var chapterData = GetChapterData(chapter);
            
            if (chapterData == null) return;
            
            // 메인 퀘스트 자동 수락
            if (chapterData.QuestIds != null)
            {
                foreach (var questId in chapterData.QuestIds)
                {
                    _questSystem?.AcceptQuest(questId);
                }
            }
            
            OnChapterStarted?.Invoke(chapter);
            
            EventBus.Publish(new ChapterStartedEvent
            {
                Chapter = chapter,
                Title = chapterData.Title,
                Description = chapterData.Description
            });
            
            Debug.Log($"[ChapterQuestManager] Chapter {chapter} started: {chapterData.Title}");
        }
        
        /// <summary>
        /// 챕터 완료
        /// </summary>
        public void CompleteChapter(ChapterType chapter)
        {
            var chapterData = GetChapterData(chapter);
            if (chapterData == null) return;
            
            chapterData.IsCompleted = true;
            
            OnChapterCompleted?.Invoke(chapter);
            
            EventBus.Publish(new ChapterCompletedEvent
            {
                Chapter = chapter,
                Title = chapterData.Title
            });
            
            Debug.Log($"[ChapterQuestManager] Chapter {chapter} completed: {chapterData.Title}");
        }
        
        /// <summary>
        /// 퀘스트 완료 시 처리
        /// </summary>
        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            CheckChapterCompletion();
        }
        
        /// <summary>
        /// 챕터 완료 체크
        /// </summary>
        private void CheckChapterCompletion()
        {
            var chapterData = GetChapterData(_currentChapter);
            if (chapterData == null || chapterData.IsCompleted) return;
            
            // 모든 메인 퀘스트 완료 체크
            if (chapterData.QuestIds != null)
            {
                bool allCompleted = true;
                foreach (var questId in chapterData.QuestIds)
                {
                    if (!_questSystem.IsQuestCompleted(questId))
                    {
                        allCompleted = false;
                        break;
                    }
                }
                
                if (allCompleted)
                {
                    CompleteChapter(_currentChapter);
                }
            }
        }
        
        /// <summary>
        /// 일일 퀘스트 생성
        /// </summary>
        private void GenerateDailyQuests()
        {
            // 현재 챕터의 서브 퀘스트 중 랜덤으로 1-2개 수락
            var chapterData = GetChapterData(_currentChapter);
            if (chapterData?.SubQuestIds != null)
            {
                var availableQuests = new List<string>();
                foreach (var questId in chapterData.SubQuestIds)
                {
                    if (!_questSystem.HasQuest(questId) && !_questSystem.IsQuestCompleted(questId))
                    {
                        availableQuests.Add(questId);
                    }
                }
                
                // 랜덤으로 1-2개 수락
                int count = UnityEngine.Random.Range(1, Mathf.Min(3, availableQuests.Count + 1));
                for (int i = 0; i < count && availableQuests.Count > 0; i++)
                {
                    int index = UnityEngine.Random.Range(0, availableQuests.Count);
                    _questSystem.AcceptQuest(availableQuests[index]);
                    availableQuests.RemoveAt(index);
                }
            }
        }
        
        /// <summary>
        /// 챕터 데이터 가져오기
        /// </summary>
        private ChapterData GetChapterData(ChapterType chapter)
        {
            foreach (var data in _chapters)
            {
                if (data.Chapter == chapter)
                    return data;
            }
            return null;
        }
        
        /// <summary>
        /// 서브 퀘스트 수락
        /// </summary>
        public void AcceptSubQuest(string questId)
        {
            _questSystem?.AcceptQuest(questId);
        }
        
        /// <summary>
        /// 현재 챕터의 진행도 (0-1)
        /// </summary>
        public float GetChapterProgress()
        {
            var chapterData = GetChapterData(_currentChapter);
            if (chapterData?.QuestIds == null || chapterData.QuestIds.Length == 0)
                return 0f;
            
            int completedCount = 0;
            foreach (var questId in chapterData.QuestIds)
            {
                if (_questSystem.IsQuestCompleted(questId))
                    completedCount++;
            }
            
            return (float)completedCount / chapterData.QuestIds.Length;
        }
        
        public object GetSaveData()
        {
            return new ChapterQuestSaveData
            {
                CurrentChapter = _currentChapter,
                CurrentDay = _currentDay,
                ChapterCompleted = GetCompletedChaptersArray()
            };
        }
        
        public void LoadSaveData(object state)
        {
            if (state is ChapterQuestSaveData data)
            {
                _currentChapter = data.CurrentChapter;
                _currentDay = data.CurrentDay;
                
                // 챕터 완료 상태 복원
                for (int i = 0; i < data.ChapterCompleted.Length && i < _chapters.Count; i++)
                {
                    _chapters[i].IsCompleted = data.ChapterCompleted[i];
                }
            }
        }
        
        private bool[] GetCompletedChaptersArray()
        {
            var result = new bool[_chapters.Count];
            for (int i = 0; i < _chapters.Count; i++)
            {
                result[i] = _chapters[i].IsCompleted;
            }
            return result;
        }
    }
    
    /// <summary>
    /// 챕터 퀘스트 저장 데이터
    /// </summary>
    [Serializable]
    public class ChapterQuestSaveData
    {
        public ChapterType CurrentChapter;
        public int CurrentDay;
        public bool[] ChapterCompleted;
    }
    
    /// <summary>
    /// 챕터 시작 이벤트
    /// </summary>
    public class ChapterStartedEvent
    {
        public ChapterType Chapter { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
    
    /// <summary>
    /// 챕터 완료 이벤트
    /// </summary>
    public class ChapterCompletedEvent
    {
        public ChapterType Chapter { get; set; }
        public string Title { get; set; }
    }
}
