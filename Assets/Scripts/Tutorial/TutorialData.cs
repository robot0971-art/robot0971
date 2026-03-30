using System;
using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Tutorial
{
    /// <summary>
    /// 튜토리얼 시퀀스 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialData", menuName = "SunnysideIsland/Tutorial/TutorialData")]
    public class TutorialData : ScriptableObject
    {
        [Header("=== Tutorial Info ===")]
        [Tooltip("튜토리얼 고유 ID")]
        public string tutorialId;
        
        [Tooltip("튜토리얼 이름")]
        public string tutorialName;
        
        [Tooltip("튜토리얼 설명")]
        [TextArea(2, 5)]
        public string description;
        
        [Header("=== Steps ===")]
        [Tooltip("튜토리얼 단계 목록")]
        public List<TutorialStep> steps = new List<TutorialStep>();
        
        [Header("=== Settings ===")]
        [Tooltip("이전 튜토리얼 완료 여부 체크")]
        public string prerequisiteTutorialId;
        
        [Tooltip("자동 시작 여부")]
        public bool autoStart;
        
        [Tooltip("자동 시작 조건 (빈 문자열이면 무조건 자동 시작)")]
        public string autoStartCondition;
        
        [Tooltip("튜토리얼 완료 보상")]
        public List<TutorialReward> rewards = new List<TutorialReward>();

        /// <summary>
        /// 전체 단계 수 반환
        /// </summary>
        public int TotalSteps => steps?.Count ?? 0;

        /// <summary>
        /// 특정 인덱스의 단계 반환
        /// </summary>
        public TutorialStep GetStep(int index)
        {
            if (steps == null || index < 0 || index >= steps.Count)
            {
                return null;
            }
            return steps[index];
        }

        /// <summary>
        /// 단계 ID로 단계 반환
        /// </summary>
        public TutorialStep GetStepById(string stepId)
        {
            if (steps == null || string.IsNullOrEmpty(stepId))
            {
                return null;
            }

            return steps.Find(step => step.stepId == stepId);
        }

        /// <summary>
        /// 유효성 검사
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(tutorialId))
            {
                Debug.LogWarning("[TutorialData] tutorialId is required");
                return false;
            }

            if (steps == null || steps.Count == 0)
            {
                Debug.LogWarning($"[TutorialData] {tutorialId} has no steps");
                return false;
            }

            foreach (var step in steps)
            {
                if (!step.IsValid())
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 튜토리얼 보상 데이터
    /// </summary>
    [Serializable]
    public class TutorialReward
    {
        [Tooltip("보상 타입")]
        public TutorialRewardType rewardType;
        
        [Tooltip("보상 ID (아이템 ID, 업적 ID 등)")]
        public string rewardId;
        
        [Tooltip("보상 수량")]
        public int amount;

        public TutorialReward()
        {
            rewardType = TutorialRewardType.Currency;
            rewardId = string.Empty;
            amount = 0;
        }

        public TutorialReward(TutorialRewardType type, string id, int qty)
        {
            rewardType = type;
            rewardId = id;
            amount = qty;
        }
    }

    /// <summary>
    /// 튜토리얼 보상 타입
    /// </summary>
    public enum TutorialRewardType
    {
        Currency,
        Item,
        Experience,
        Achievement,
        UnlockFeature
    }
}