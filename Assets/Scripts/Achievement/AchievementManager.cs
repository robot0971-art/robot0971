using System;
using System.Collections.Generic;
using UnityEngine;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.Achievement
{
    /// <summary>
    /// 업적 진행 상황
    /// </summary>
    [Serializable]
    public class AchievementProgress
    {
        public string AchievementId;
        public int CurrentValue;
        public AchievementState State;
        public DateTime UnlockedTime;

        public AchievementProgress()
        {
            AchievementId = string.Empty;
            CurrentValue = 0;
            State = AchievementState.Locked;
            UnlockedTime = DateTime.MinValue;
        }

        public AchievementProgress(string achievementId)
        {
            AchievementId = achievementId;
            CurrentValue = 0;
            State = AchievementState.Locked;
            UnlockedTime = DateTime.MinValue;
        }
    }

    /// <summary>
    /// 업적 매니저 인터페이스
    /// </summary>
    public interface IAchievementManager
    {
        void UpdateProgress(string achievementId, int amount);
        void UnlockAchievement(string achievementId);
        bool ClaimReward(string achievementId);
        List<AchievementData> GetAchievementsByType(AchievementType type);
        List<AchievementData> GetUnlockedAchievements();
        AchievementProgress GetProgress(string achievementId);
        bool IsUnlocked(string achievementId);
        bool IsClaimed(string achievementId);
    }

    /// <summary>
    /// 업적 시스템 매니저
    /// </summary>
    public class AchievementManager : MonoBehaviour, IAchievementManager, ISaveable
    {
        [Header("=== Achievement Database ===")]
        [SerializeField] private List<AchievementData> _achievementDatabase = new List<AchievementData>();

        private Dictionary<string, AchievementProgress> _progressDict = new Dictionary<string, AchievementProgress>();
        private HashSet<string> _claimedAchievements = new HashSet<string>();

        public string SaveKey => "AchievementManager";

        public int TotalAchievements => _achievementDatabase.Count;
        public int UnlockedCount => GetUnlockedAchievements().Count;
        public int ClaimedCount => _claimedAchievements.Count;

        private void Awake()
        {
            InitializeProgress();
        }

        private void Start()
        {
            DIContainer.Inject(this);
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        /// <summary>
        /// 진행 상황 초기화
        /// </summary>
        private void InitializeProgress()
        {
            _progressDict.Clear();

            foreach (var achievement in _achievementDatabase)
            {
                if (achievement != null && !string.IsNullOrEmpty(achievement.AchievementId))
                {
                    if (!_progressDict.ContainsKey(achievement.AchievementId))
                    {
                        _progressDict[achievement.AchievementId] = new AchievementProgress(achievement.AchievementId);
                    }
                }
            }
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeEvents()
        {
            EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        /// <summary>
        /// 업적 진행 상황 업데이트
        /// </summary>
        public void UpdateProgress(string achievementId, int amount)
        {
            if (string.IsNullOrEmpty(achievementId)) return;

            var achievement = GetAchievementData(achievementId);
            if (achievement == null) return;

            if (!_progressDict.TryGetValue(achievementId, out var progress))
            {
                progress = new AchievementProgress(achievementId);
                _progressDict[achievementId] = progress;
            }

            if (progress.State == AchievementState.Unlocked || progress.State == AchievementState.Claimed)
            {
                return;
            }

            progress.CurrentValue += amount;
            progress.State = AchievementState.InProgress;

            if (progress.CurrentValue >= achievement.TargetValue)
            {
                UnlockAchievement(achievementId);
            }
            else
            {
                EventBus.Publish(new AchievementProgressEvent
                {
                    AchievementId = achievementId,
                    CurrentValue = progress.CurrentValue,
                    TargetValue = achievement.TargetValue
                });
            }
        }

        /// <summary>
        /// 업적 달성
        /// </summary>
        public void UnlockAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return;

            var achievement = GetAchievementData(achievementId);
            if (achievement == null) return;

            if (!_progressDict.TryGetValue(achievementId, out var progress))
            {
                progress = new AchievementProgress(achievementId);
                _progressDict[achievementId] = progress;
            }

            if (progress.State == AchievementState.Unlocked || progress.State == AchievementState.Claimed)
            {
                return;
            }

            progress.State = AchievementState.Unlocked;
            progress.UnlockedTime = DateTime.Now;
            progress.CurrentValue = achievement.TargetValue;

            Debug.Log($"[AchievementManager] Achievement Unlocked: {achievement.Title}");

            EventBus.Publish(new AchievementUnlockedEvent
            {
                AchievementId = achievementId,
                Title = achievement.Title,
                IsHidden = achievement.Hidden
            });
        }

        /// <summary>
        /// 보상 수령
        /// </summary>
        public bool ClaimReward(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return false;

            if (!_progressDict.TryGetValue(achievementId, out var progress))
            {
                return false;
            }

            if (progress.State != AchievementState.Unlocked)
            {
                return false;
            }

            var achievement = GetAchievementData(achievementId);
            if (achievement == null || !achievement.HasRewards())
            {
                return false;
            }

            foreach (var reward in achievement.Rewards)
            {
                GrantReward(reward);
            }

            progress.State = AchievementState.Claimed;
            _claimedAchievements.Add(achievementId);

            EventBus.Publish(new AchievementClaimedEvent
            {
                AchievementId = achievementId,
                Rewards = achievement.Rewards
            });

            Debug.Log($"[AchievementManager] Reward Claimed: {achievement.Title}");
            return true;
        }

        /// <summary>
        /// 보상 지급
        /// </summary>
        private void GrantReward(AchievementReward reward)
        {
            switch (reward.RewardType)
            {
                case AchievementRewardType.Gold:
                    break;

                case AchievementRewardType.Item:
                    EventBus.Publish(new GrantItemEvent
                    {
                        ItemId = reward.RewardId,
                        Amount = reward.Amount
                    });
                    break;

                case AchievementRewardType.Experience:
                    EventBus.Publish(new GrantExperienceEvent
                    {
                        Amount = reward.Amount
                    });
                    break;

                case AchievementRewardType.UnlockFeature:
                    EventBus.Publish(new UnlockFeatureEvent
                    {
                        FeatureId = reward.RewardId
                    });
                    break;
            }
        }

        /// <summary>
        /// 타입별 업적 조회
        /// </summary>
        public List<AchievementData> GetAchievementsByType(AchievementType type)
        {
            var result = new List<AchievementData>();

            foreach (var achievement in _achievementDatabase)
            {
                if (achievement != null && achievement.Type == type)
                {
                    result.Add(achievement);
                }
            }

            result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return result;
        }

        /// <summary>
        /// 달성한 업적 목록 조회
        /// </summary>
        public List<AchievementData> GetUnlockedAchievements()
        {
            var result = new List<AchievementData>();

            foreach (var achievement in _achievementDatabase)
            {
                if (achievement != null && IsUnlocked(achievement.AchievementId))
                {
                    result.Add(achievement);
                }
            }

            return result;
        }

        /// <summary>
        /// 진행 상황 조회
        /// </summary>
        public AchievementProgress GetProgress(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return null;

            _progressDict.TryGetValue(achievementId, out var progress);
            return progress;
        }

        /// <summary>
        /// 업적 달성 여부 확인
        /// </summary>
        public bool IsUnlocked(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return false;

            if (_progressDict.TryGetValue(achievementId, out var progress))
            {
                return progress.State == AchievementState.Unlocked || progress.State == AchievementState.Claimed;
            }
            return false;
        }

        /// <summary>
        /// 보상 수령 여부 확인
        /// </summary>
        public bool IsClaimed(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return false;
            return _claimedAchievements.Contains(achievementId);
        }

        /// <summary>
        /// 업적 데이터 조회
        /// </summary>
        public AchievementData GetAchievementData(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return null;

            foreach (var achievement in _achievementDatabase)
            {
                if (achievement != null && achievement.AchievementId == achievementId)
                {
                    return achievement;
                }
            }
            return null;
        }

        /// <summary>
        /// 모든 업적 데이터 조회
        /// </summary>
        public List<AchievementData> GetAllAchievements()
        {
            return new List<AchievementData>(_achievementDatabase);
        }

        #region Event Handlers

        private void OnItemPickedUp(ItemPickedUpEvent evt)
        {
            foreach (var achievement in _achievementDatabase)
            {
                if (achievement != null && achievement.Type == AchievementType.Collection)
                {
                    if (string.IsNullOrEmpty(achievement.RelatedId) || achievement.RelatedId == evt.ItemId)
                    {
                        UpdateProgress(achievement.AchievementId, evt.Quantity);
                    }
                }
            }
        }

        private void OnPlayerLevelUp(PlayerLevelUpEvent evt)
        {
            foreach (var achievement in _achievementDatabase)
            {
                if (achievement != null && achievement.Type == AchievementType.Progress)
                {
                    UpdateProgress(achievement.AchievementId, 1);
                }
            }
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            foreach (var achievement in _achievementDatabase)
            {
                if (achievement != null && achievement.Type == AchievementType.Combat)
                {
                    if (string.IsNullOrEmpty(achievement.RelatedId) || achievement.RelatedId == evt.EnemyId)
                    {
                        UpdateProgress(achievement.AchievementId, 1);
                    }
                }
            }
        }

        #endregion

        #region ISaveable

        public object GetSaveData()
        {
            return new AchievementSaveData
            {
                ProgressList = new List<AchievementProgress>(_progressDict.Values),
                ClaimedAchievements = new List<string>(_claimedAchievements)
            };
        }

        public void LoadSaveData(object data)
        {
            if (data is AchievementSaveData saveData)
            {
                InitializeProgress();

                if (saveData.ProgressList != null)
                {
                    foreach (var progress in saveData.ProgressList)
                    {
                        if (progress != null && !string.IsNullOrEmpty(progress.AchievementId))
                        {
                            _progressDict[progress.AchievementId] = progress;
                        }
                    }
                }

                _claimedAchievements.Clear();
                if (saveData.ClaimedAchievements != null)
                {
                    foreach (var claimed in saveData.ClaimedAchievements)
                    {
                        _claimedAchievements.Add(claimed);
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 업적 저장 데이터
    /// </summary>
    [Serializable]
    public class AchievementSaveData
    {
        public List<AchievementProgress> ProgressList;
        public List<string> ClaimedAchievements;
    }

    #region Events

    /// <summary>
    /// 업적 진행 이벤트
    /// </summary>
    public class AchievementProgressEvent
    {
        public string AchievementId { get; set; }
        public int CurrentValue { get; set; }
        public int TargetValue { get; set; }
    }

    /// <summary>
    /// 업적 달성 이벤트
    /// </summary>
    public class AchievementUnlockedEvent
    {
        public string AchievementId { get; set; }
        public string Title { get; set; }
        public bool IsHidden { get; set; }
    }

    /// <summary>
    /// 업적 보상 수령 이벤트
    /// </summary>
    public class AchievementClaimedEvent
    {
        public string AchievementId { get; set; }
        public List<AchievementReward> Rewards { get; set; }
    }

    /// <summary>
    /// 아이템 지급 이벤트
    /// </summary>
    public class GrantItemEvent
    {
        public string ItemId { get; set; }
        public int Amount { get; set; }
    }

    /// <summary>
    /// 경험치 지급 이벤트
    /// </summary>
    public class GrantExperienceEvent
    {
        public int Amount { get; set; }
    }

    /// <summary>
    /// 기능 해금 이벤트
    /// </summary>
    public class UnlockFeatureEvent
    {
        public string FeatureId { get; set; }
    }

    /// <summary>
    /// 적 처치 이벤트
    /// </summary>
    public class EnemyDefeatedEvent
    {
        public string EnemyId { get; set; }
        public int Level { get; set; }
    }

    #endregion
}