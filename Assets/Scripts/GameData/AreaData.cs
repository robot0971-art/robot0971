using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class AreaData
    {
        [Column("AreaID")]
        public string areaId;
        
        [Column("AreaName")]
        public string areaName;
        
        [Column("AreaType")]
        public AreaType areaType;
        
        [Column("Difficulty")]
        public int difficulty;
        
        [Column("AvailableResources")]
        public string availableResources;
        
        [Column("AvailableAnimals")]
        public string availableAnimals;
        
        [Column("AvailableMonsters")]
        public string availableMonsters;
    }

    public enum AreaType
    {
        Beach,
        Forest,
        Mountain,
        Ocean,
        Cave,
        GoblinVillage
    }
}
