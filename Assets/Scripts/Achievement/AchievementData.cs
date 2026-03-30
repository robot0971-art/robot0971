using System;
using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Achievement
{
    /// <summary>
    /// 업적 타입
    /// </summary>
    public enum AchievementType
    {
        Progress,
        Collection,
        Survival,
        Combat,
        Economy,
        Social
    }

    /// <summary>
    /// 업적 상태
    /// </summary>
    public enum AchievementState
    {
        Locked,
        InProgress,
        Unlocked,
        Claimed
    }

    /// <summary>
    /// 업적 보상
    /// </summary>
    [Serializable]
    public class AchievementReward
    {
        [Tooltip("보상 타입")]
        public AchievementRewardType RewardType;

        [Tooltip("보상 ID (아이템 ID 등)")]
        public string RewardId;

        [Tooltip("보상 수량")]
        public int Amount;

        public AchievementReward()
        {
            RewardType = AchievementRewardType.Gold;
            RewardId = string.Empty;
            Amount = 0;
        }

        public AchievementReward(AchievementRewardType type, string id, int amount)
        {
            RewardType = type;
            RewardId = id;
            Amount = amount;
        }
    }

    /// <summary>
    /// 업적 보상 타입
    /// </summary>
    public enum AchievementRewardType
    {
        Gold,
        Item,
        Experience,
        UnlockFeature
    }

    /// <summary>
    /// 업적 정의 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "AchievementData", menuName = "SunnysideIsland/Achievement/AchievementData")]
    public class AchievementData : ScriptableObject
    {
        [Header("=== Basic Info ===")]
        [Tooltip("업적 고유 ID")]
        public string AchievementId;

        [Tooltip("업적 제목")]
        public string Title;

        [Tooltip("업적 설명")]
        [TextArea(2, 4)]
        public string Description;

        [Tooltip("업적 아이콘")]
        public Sprite Icon;

        [Header("=== Type & Target ===")]
        [Tooltip("업적 타입")]
        public AchievementType Type;

        [Tooltip("목표 값 (예: 100회 수집, 50마리 처치)")]
        public int TargetValue;

        [Tooltip("연관된 ID (아이템 ID, 몬스터 ID 등)")]
        public string RelatedId;

        [Header("=== Rewards ===")]
        [Tooltip("업적 보상 목록")]
        public List<AchievementReward> Rewards = new List<AchievementReward>();

        [Header("=== Settings ===")]
        [Tooltip("숨겨진 업적 (비밀 업적)")]
        public bool Hidden;

        [Tooltip("선행 업적 ID")]
        public string PrerequisiteAchievementId;

        [Tooltip("정렬 순서")]
        public int SortOrder;

        /// <summary>
        /// 유효성 검사
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(AchievementId))
            {
                Debug.LogWarning("[AchievementData] AchievementId is required");
                return false;
            }

            if (string.IsNullOrEmpty(Title))
            {
                Debug.LogWarning($"[AchievementData] {AchievementId} has no title");
                return false;
            }

            if (TargetValue <= 0)
            {
                Debug.LogWarning($"[AchievementData] {AchievementId} has invalid TargetValue");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 보상이 있는지 확인
        /// </summary>
        public bool HasRewards()
        {
            return Rewards != null && Rewards.Count > 0;
        }
    }
}