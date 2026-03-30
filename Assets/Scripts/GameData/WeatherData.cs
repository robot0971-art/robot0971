using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class WeatherData
    {
        [Column("WeatherType")]
        public WeatherType weatherType;
        
        [Column("Probability")]
        public float probability;
        
        [Column("Effect")]
        public string effect;
    }

    public enum WeatherType
    {
        Sunny,
        Cloudy,
        Rainy,
        Stormy,
        Rainbow
    }
}
