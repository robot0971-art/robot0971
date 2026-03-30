using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class FishData
    {
        [Column("FishID")]
        public string fishId;
        
        [Column("FishName")]
        public string fishName;
        
        [Column("Grade")]
        public int grade;
        
        [Column("Location")]
        public string location;
        
        [Column("TimeCondition")]
        public string timeCondition;
        
        [Column("Difficulty")]
        public FishDifficulty difficulty;
        
        [Column("SellPrice")]
        public int sellPrice;
        
        [Column("HungerRestore")]
        public int hungerRestore;
        
        [Column("ItemID")]
        public string itemId;
    }

    public enum FishDifficulty
    {
        Easy,
        Normal,
        Hard,
        VeryHard,
        Extreme
    }
}
