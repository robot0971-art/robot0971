using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class QuestData
    {
        [Column("QuestID")]
        public string questId;
        
        [Column("QuestName")]
        public string questName;
        
        [Column("QuestType")]
        public QuestType questType;
        
        [Column("Chapter")]
        public int chapter;
        
        [Column("Description")]
        public string description;
        
        [Column("Objectives")]
        public string objectives;
        
        [Column("Rewards")]
        public string rewards;
        
        [Column("Prerequisites")]
        public string prerequisites;
    }

    public enum QuestType
    {
        Main,
        Sub,
        Daily
    }
}
