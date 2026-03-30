using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class CommercialBuildingData
    {
        [Column("BuildingID")]
        public string buildingId;
        
        [Column("DailyIncomeMin")]
        public int dailyIncomeMin;
        
        [Column("DailyIncomeMax")]
        public int dailyIncomeMax;
        
        [Column("SpecialEffect")]
        public string specialEffect;
    }
}
