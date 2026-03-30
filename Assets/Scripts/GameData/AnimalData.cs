using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [CreateAssetMenu(fileName = "AnimalData", menuName = "GameData/Animal Data")]
    public class AnimalData : ScriptableObject
    {
        [Column("AnimalID")]
        public string animalId;
        
        [Column("AnimalName")]
        public string animalName;
        
        [Column("HP")]
        public int hp;
        
        [Column("AttackPower")]
        public int attackPower;
        
        [Column("Speed")]
        public AnimalSpeed speed;
        
        [Column("AIType")]
        public AnimalAIType aiType;
        
        [Column("DropItems")]
        public string dropItems;
    }

    public enum AnimalSpeed
    {
        Slow,
        Normal,
        Fast,
        VeryFast
    }

    public enum AnimalAIType
    {
        Flee,
        Evasive,
        Hostile,
        Territorial
    }
}
