using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class BossData
    {
        [Column("BossID")]
        public string bossId;
        
        [Column("BossName")]
        public string bossName;
        
        [Column("PhaseCount")]
        public int phaseCount;
        
        [Column("PhaseHPThresholds")]
        public string phaseHPThresholds;
        
        [Column("Phase1Skills")]
        public string phase1Skills;
        
        [Column("Phase2Skills")]
        public string phase2Skills;
        
        [Column("Phase3Skills")]
        public string phase3Skills;
        
        [Column("Rewards")]
        public string rewards;
        
        [Column("MonsterID")]
        public string monsterId;
    }
}
