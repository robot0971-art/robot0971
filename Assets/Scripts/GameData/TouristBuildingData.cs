using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class TouristBuildingData
    {
        [Column("BuildingID")]
        public string buildingId;
        
        [Column("TouristIncrease")]
        public float touristIncrease;
    }
}
