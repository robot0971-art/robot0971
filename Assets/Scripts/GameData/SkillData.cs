using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class SkillData
    {
        [Column("SkillID")]
        public string skillId;
        
        [Column("SkillTree")]
        public SkillTree skillTree;
        
        [Column("Level")]
        public int level;
        
        [Column("SkillName")]
        public string skillName;
        
        [Column("Effect")]
        public string effect;
        
        [Column("Value")]
        public float value;
    }

    public enum SkillTree
    {
        Gathering,
        Farming,
        Fishing,
        Combat,
        ConstructionEconomy
    }
}
