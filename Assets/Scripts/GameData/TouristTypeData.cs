using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class TouristTypeData
    {
        [Column("TypeID")]
        public string typeId;
        
        [Column("TypeName")]
        public string typeName;
        
        [Column("Ratio")]
        public float ratio;
        
        [Column("PreferredFacilities")]
        public string preferredFacilities;
        
        [Column("SpendingMin")]
        public int spendingMin;
        
        [Column("SpendingMax")]
        public int spendingMax;
    }
}
