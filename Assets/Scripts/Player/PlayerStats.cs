using System;
using System.Collections.Generic;
using UnityEngine;
using DI;
using SunnysideIsland.Events;
using SunnysideIsland.Core;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Player
{
    public interface IPlayerStats
    {
        int Level { get; }
        int Experience { get; }
        int MaxExperience { get; }
        int AttackPower { get; }
        int Defense { get; }
        float MoveSpeedMultiplier { get; }
        
        void GainExperience(int amount);
        void AddStatBonus(StatType stat, float value);
        void RemoveStatBonus(StatType stat, float value);
    }

    public enum StatType
    {
        AttackPower,
        Defense,
        MoveSpeed,
        CriticalChance,
        CriticalDamage,
        GatheringSpeed,
        FarmingSpeed,
        FishingLuck,
        CraftingQuality
    }

    public class PlayerStats : MonoBehaviour, IPlayerStats, ISaveable
    {
        public static PlayerStats Instance { get; private set; }

        [Header("=== Level Settings ===")]
        [SerializeField] private int _maxLevel = 10;
        [SerializeField] private int _baseExpRequired = 100;
        [SerializeField] private float _expMultiplier = 2f;

        [Header("=== Base Stats ===")]
        [SerializeField] private int _baseAttackPower = 10;
        [SerializeField] private int _baseDefense = 0;
        [SerializeField] private float _baseMoveSpeed = 1f;
        [SerializeField] private float _baseCriticalChance = 0.05f;
        [SerializeField] private float _baseCriticalDamage = 1.5f;

        private int _level = 1;
        private int _experience = 0;
        private int _skillPoints = 0;
        private Dictionary<StatType, float> _statBonuses = new Dictionary<StatType, float>();
        private Dictionary<SkillTree, int> _skillLevels = new Dictionary<SkillTree, int>();

        public int Level => _level;
        public int Experience => _experience;
        public int MaxExperience => CalculateExpRequired(_level);
        public int SkillPoints => _skillPoints;
        public string SaveKey => "PlayerStats";

        public int AttackPower => _baseAttackPower + Mathf.RoundToInt(GetStatBonus(StatType.AttackPower));
        public int Defense => _baseDefense + Mathf.RoundToInt(GetStatBonus(StatType.Defense));
        public float MoveSpeedMultiplier => _baseMoveSpeed + GetStatBonus(StatType.MoveSpeed);
        public float CriticalChance => _baseCriticalChance + GetStatBonus(StatType.CriticalChance);
        public float CriticalDamage => _baseCriticalDamage + GetStatBonus(StatType.CriticalDamage);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (SkillTree tree in Enum.GetValues(typeof(SkillTree)))
            {
                if (!_skillLevels.ContainsKey(tree))
                    _skillLevels[tree] = 0;
            }
        }

        public void GainExperience(int amount)
        {
            if (_level >= _maxLevel) return;

            _experience += amount;

            EventBus.Publish(new ExperienceGainedEvent
            {
                Amount = amount,
                TotalExperience = _experience
            });

            while (_experience >= MaxExperience && _level < _maxLevel)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            _experience -= MaxExperience;
            _level++;
            _skillPoints++;

            EventBus.Publish(new LevelUpEvent
            {
                NewLevel = _level,
                SkillPointsGained = 1
            });

            Debug.Log($"[PlayerStats] Level Up! Now level {_level}");
        }

        private int CalculateExpRequired(int level)
        {
            return Mathf.RoundToInt(_baseExpRequired * Mathf.Pow(_expMultiplier, level - 1));
        }

        public bool UpgradeSkill(SkillTree skillTree)
        {
            if (_skillPoints <= 0) return false;
            if (_skillLevels[skillTree] >= 5) return false;

            _skillPoints--;
            _skillLevels[skillTree]++;

            ApplySkillEffect(skillTree, _skillLevels[skillTree]);

            EventBus.Publish(new SkillUpgradedEvent
            {
                SkillTree = skillTree,
                NewLevel = _skillLevels[skillTree]
            });

            return true;
        }

        private void ApplySkillEffect(SkillTree skillTree, int level)
        {
            switch (skillTree)
            {
                case SkillTree.Gathering:
                    AddStatBonus(StatType.GatheringSpeed, level * 0.1f);
                    break;
                case SkillTree.Farming:
                    AddStatBonus(StatType.FarmingSpeed, level * 0.1f);
                    break;
                case SkillTree.Fishing:
                    AddStatBonus(StatType.FishingLuck, level * 0.1f);
                    break;
                case SkillTree.Combat:
                    AddStatBonus(StatType.AttackPower, level * 0.1f * _baseAttackPower);
                    AddStatBonus(StatType.Defense, level * 0.1f * _baseDefense);
                    break;
                case SkillTree.ConstructionEconomy:
                    AddStatBonus(StatType.CraftingQuality, level * 0.1f);
                    break;
            }
        }

        public int GetSkillLevel(SkillTree skillTree)
        {
            return _skillLevels.TryGetValue(skillTree, out int level) ? level : 0;
        }

        public void AddStatBonus(StatType stat, float value)
        {
            if (!_statBonuses.ContainsKey(stat))
                _statBonuses[stat] = 0f;
            _statBonuses[stat] += value;
        }

        public void RemoveStatBonus(StatType stat, float value)
        {
            if (_statBonuses.ContainsKey(stat))
                _statBonuses[stat] -= value;
        }

        public float GetStatBonus(StatType stat)
        {
            return _statBonuses.TryGetValue(stat, out float value) ? value : 0f;
        }

        #region ISaveable

        public object GetSaveData()
        {
            return new PlayerStatsSaveData
            {
                Level = _level,
                Experience = _experience,
                SkillPoints = _skillPoints,
                SkillLevels = new Dictionary<SkillTree, int>(_skillLevels)
            };
        }

        public void LoadSaveData(object data)
        {
            if (data is PlayerStatsSaveData saveData)
            {
                _level = saveData.Level;
                _experience = saveData.Experience;
                _skillPoints = saveData.SkillPoints;
                
                if (saveData.SkillLevels != null)
                {
                    foreach (var kvp in saveData.SkillLevels)
                    {
                        _skillLevels[kvp.Key] = kvp.Value;
                        ApplySkillEffect(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        [Serializable]
        public class PlayerStatsSaveData
        {
            public int Level;
            public int Experience;
            public int SkillPoints;
            public Dictionary<SkillTree, int> SkillLevels;
        }

        #endregion
    }

    #region Events

    public class ExperienceGainedEvent
    {
        public int Amount { get; set; }
        public int TotalExperience { get; set; }
    }

    public class LevelUpEvent
    {
        public int NewLevel { get; set; }
        public int SkillPointsGained { get; set; }
    }

    public class SkillUpgradedEvent
    {
        public SkillTree SkillTree { get; set; }
        public int NewLevel { get; set; }
    }

    #endregion
}