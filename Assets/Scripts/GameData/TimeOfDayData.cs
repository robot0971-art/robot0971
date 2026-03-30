using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class TimeOfDayData
    {
        [Column("TimeSlot")]
        public TimeSlot timeSlot;
        
        [Column("StartHour")]
        public int startHour;
        
        [Column("EndHour")]
        public int endHour;
        
        [Column("Feature")]
        public string feature;
        
        [Column("RecommendedActivity")]
        public string recommendedActivity;
    }

    public enum TimeSlot
    {
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Evening,
        Night
    }
}
