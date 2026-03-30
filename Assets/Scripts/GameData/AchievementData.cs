using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class AchievementData
    {
        [Column("AchievementID")]
        public string achievementId;
        
        [Column("AchievementName")]
        public string achievementName;
        
        [Column("Category")]
        public AchievementCategory category;
        
        [Column("RequirementType")]
        public RequirementType requirementType;
        
        [Column("TargetID")]
        public string targetId;
        
        [Column("TargetAmount")]
        public int targetAmount;
        
        [Column("Reward")]
        public string reward;
    }

    public enum AchievementCategory
    {
        Gathering,
        Farming,
        Fishing,
        Combat,
        Building,
        Economy,
        Special
    }

    public enum RequirementType
    {
        Collect,
        Defeat,
        Build,
        Own,
        Craft,
        Cook
    }
}
