using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class ResidentData
    {
        [Column("ResidentID")]
        public string residentId;
        
        [Column("ResidentType")]
        public ResidentType residentType;
        
        [Column("Function")]
        public string function;
        
        [Column("DailyWage")]
        public int dailyWage;
        
        [Column("SpecialAbility")]
        public string specialAbility;
        
        [Column("RequiredCondition")]
        public string requiredCondition;
    }

    public enum ResidentType
    {
        Merchant,
        Blacksmith,
        Chef,
        Farmer,
        Carpenter,
        Guard,
        Researcher,
        Guide
    }
}
