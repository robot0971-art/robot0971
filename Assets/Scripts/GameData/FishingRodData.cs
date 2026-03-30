using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class FishingRodData
    {
        [Column("RodID")]
        public string rodId;
        
        [Column("RodName")]
        public string rodName;
        
        [Column("SuccessRate")]
        public float successRate;
        
        [Column("Durability")]
        public int durability;
        
        [Column("RareFishBonus")]
        public float rareFishBonus;
        
        [Column("ItemID")]
        public string itemId;
    }
}
