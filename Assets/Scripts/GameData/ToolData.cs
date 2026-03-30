using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class ToolData
    {
        [Column("ToolID")]
        public string toolId;
        
        [Column("ToolName")]
        public string toolName;
        
        [Column("ToolType")]
        public ToolType toolType;
        
        [Column("Durability")]
        public int durability;
        
        [Column("RepairCost")]
        public int repairCost;
        
        [Column("ItemID")]
        public string itemId;
        
        [Column("EffectValue")]
        public float effectValue;
    }

    public enum ToolType
    {
        Axe,
        Pickaxe,
        FishingRod,
        Hoe,
        WateringCan
    }
}
