using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class MonsterData
    {
        [Column("MonsterID")]
        public string monsterId;
        
        [Column("MonsterName")]
        public string monsterName;
        
        [Column("HP")]
        public int hp;
        
        [Column("AttackPower")]
        public int attackPower;
        
        [Column("Defense")]
        public int defense;
        
        [Column("Speed")]
        public MonsterSpeed speed;
        
        [Column("DropItems")]
        public string dropItems;
        
        [Column("ExpReward")]
        public int expReward;
    }

    public enum MonsterSpeed
    {
        Slow,
        Normal,
        Fast
    }
}
