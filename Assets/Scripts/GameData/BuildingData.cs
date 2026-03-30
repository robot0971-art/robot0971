using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class BuildingData
    {
        [Column("BuildingID")]
        public string buildingId;
        
        [Column("BuildingName")]
        public string buildingName;
        
        [Column("BuildingType")]
        public BuildingType buildingType;
        
        [Column("SizeX")]
        public int sizeX;
        
        [Column("SizeY")]
        public int sizeY;
        
        [Column("BuildTime")]
        public int buildTime;
        
        [Column("RequiredResources")]
        public string requiredResources;
        
        [Column("EffectDescription")]
        public string effectDescription;
    }

    public enum BuildingType
    {
        Residential,
        Agriculture,
        Commercial,
        Tourist,
        Production
    }
}
