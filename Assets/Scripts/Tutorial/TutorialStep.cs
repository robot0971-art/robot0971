using System;
using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Tutorial
{
    /// <summary>
    /// 튜토리얼 단계에서 필요한 액션 타입
    /// </summary>
    public enum TutorialActionType
    {
        Click,
        PressKey,
        Wait,
        CompleteTask
    }

    /// <summary>
    /// 튜토리얼 단계 데이터
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        [Header("=== Step Info ===")]
        [Tooltip("단계 고유 ID")]
        public string stepId;
        
        [Tooltip("단계 제목")]
        public string title;
        
        [Tooltip("단계 설명")]
        [TextArea(3, 10)]
        public string description;
        
        [Header("=== Highlight ===")]
        [Tooltip("하이라이트할 UI 요소 경로 목록")]
        public List<string> highlightTargets = new List<string>();
        
        [Header("=== Action ===")]
        [Tooltip("필요한 액션 타입")]
        public TutorialActionType requiredAction;
        
        [Tooltip("액션 대상 (키 코드, 버튼 이름 등)")]
        public string actionTarget;
        
        [Tooltip("액션 완료 대기 시간 (Wait 타입용)")]
        public float delay;
        
        [Header("=== Settings ===")]
        [Tooltip("자동으로 다음 단계로 진행")]
        public bool autoAdvance;
        
        [Tooltip("다음 단계로 진행하기 전 대기 시간")]
        public float advanceDelay = 1f;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public TutorialStep()
        {
            stepId = string.Empty;
            title = string.Empty;
            description = string.Empty;
            requiredAction = TutorialActionType.Click;
            actionTarget = string.Empty;
            delay = 0f;
            autoAdvance = false;
            advanceDelay = 1f;
        }

        /// <summary>
        /// 단순 텍스트 표시용 생성자
        /// </summary>
        public TutorialStep(string id, string stepTitle, string stepDescription)
        {
            stepId = id;
            title = stepTitle;
            description = stepDescription;
            requiredAction = TutorialActionType.Click;
            actionTarget = string.Empty;
            delay = 0f;
            autoAdvance = false;
            advanceDelay = 1f;
        }

        /// <summary>
        /// 전체 파라미터 생성자
        /// </summary>
        public TutorialStep(
            string id,
            string stepTitle,
            string stepDescription,
            TutorialActionType actionType,
            string target = "",
            float waitDelay = 0f)
        {
            stepId = id;
            title = stepTitle;
            description = stepDescription;
            requiredAction = actionType;
            actionTarget = target;
            delay = waitDelay;
            autoAdvance = false;
            advanceDelay = 1f;
        }

        /// <summary>
        /// 유효성 검사
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(stepId))
            {
                Debug.LogWarning("[TutorialStep] stepId is required");
                return false;
            }

            if (requiredAction == TutorialActionType.PressKey && string.IsNullOrEmpty(actionTarget))
            {
                Debug.LogWarning($"[TutorialStep] actionTarget is required for PressKey action: {stepId}");
                return false;
            }

            return true;
        }
    }
}