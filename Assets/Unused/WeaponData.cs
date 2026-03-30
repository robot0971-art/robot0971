using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class WeaponData
    {
        [Column("WeaponID")]
        public string weaponId;
        
        [Column("WeaponName")]
        public string weaponName;
        
        [Column("AttackPower")]
        public int attackPower;
        
        [Column("AttackSpeed")]
        public AttackSpeed attackSpeed;
        
        [Column("RangeType")]
        public RangeType rangeType;
        
        [Column("SpecialEffect")]
        public string specialEffect;
        
        [Column("RecipeID")]
        public string recipeId;
        
        [Column("ItemID")]
        public string itemId;
    }

    public enum AttackSpeed
    {
        VerySlow,
        Slow,
        Normal,
        Fast,
        VeryFast
    }

    public enum RangeType
    {
        Melee,
        MidRange,
        LongRange
    }
}
