using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class EventData
    {
        [Column("EventID")]
        public string eventId;
        
        [Column("EventName")]
        public string eventName;
        
        [Column("EventType")]
        public EventType eventType;
        
        [Column("TriggerCondition")]
        public string triggerCondition;
        
        [Column("Probability")]
        public float probability;
        
        [Column("Effect")]
        public string effect;
        
        [Column("Duration")]
        public int duration;
    }

    public enum EventType
    {
        Weekly,
        Seasonal,
        Random
    }
}
