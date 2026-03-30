using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class ResourceSpawnData
    {
        [Column("ResourceID")]
        public string resourceId;
        
        [Column("ResourceName")]
        public string resourceName;
        
        [Column("RegenDays")]
        public int regenDays;
        
        [Column("MaxQuantity")]
        public int maxQuantity;
        
        [Column("SpawnAreas")]
        public string spawnAreas;
    }
}
