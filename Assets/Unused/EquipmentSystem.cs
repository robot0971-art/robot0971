using System;
using System.Collections.Generic;
using UnityEngine;
using DI;
using SunnysideIsland.Events;
using SunnysideIsland.Core;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Equipment
{
    public interface IEquipmentSystem
    {
        WeaponData EquippedWeapon { get; }
        ArmorData EquippedArmor { get; }
        AccessoryData[] EquippedAccessories { get; }
        int TotalAttackPower { get; }
        int TotalDefense { get; }
        
        bool Equip(EquipmentData equipment);
        void Unequip(EquipmentSlotType slotType, int slotIndex = 0);
        bool CanEquip(EquipmentData equipment);
    }

    public enum EquipmentSlotType
    {
        Weapon,
        Armor,
        Accessory
    }

    public enum EquipmentRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public class EquipmentData : ScriptableObject
    {
        public string EquipmentId;
        public string EquipmentName;
        public EquipmentSlotType SlotType;
        public EquipmentRarity Rarity;
        public int RequiredLevel;
        public Sprite Icon;
        
        [TextArea(2, 4)]
        public string Description;
    }

    [Serializable]
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Sunnyside/Weapon Data")]
    public class WeaponData : EquipmentData
    {
        public int AttackPower;
        public float AttackSpeed;
        public float AttackRange;
        public WeaponType WeaponType;
        public AttackType AttackType;
    }

    public enum WeaponType
    {
        Sword,
        Axe,
        Spear,
        Bow,
        Staff
    }

    public enum AttackType
    {
        Melee,
        Ranged,
        Magic
    }

    [Serializable]
    [CreateAssetMenu(fileName = "ArmorData", menuName = "Sunnyside/Armor Data")]
    public class ArmorData : EquipmentData
    {
        public int Defense;
        public int MaxHealthBonus;
        public ArmorType ArmorType;
    }

    public enum ArmorType
    {
        Light,
        Medium,
        Heavy
    }

    [Serializable]
    [CreateAssetMenu(fileName = "AccessoryData", menuName = "Sunnyside/Accessory Data")]
    public class AccessoryData : EquipmentData
    {
        public int AttackBonus;
        public int DefenseBonus;
        public float CriticalChanceBonus;
        public float MoveSpeedBonus;
        public AccessoryType AccessoryType;
    }

    public enum AccessoryType
    {
        Ring,
        Amulet,
        Bracelet,
        Belt
    }

    public class EquipmentSystem : MonoBehaviour, IEquipmentSystem, ISaveable
    {
        public static EquipmentSystem Instance { get; private set; }

        [Header("=== Settings ===")]
        [SerializeField] private int _accessorySlotCount = 2;

        private WeaponData _equippedWeapon;
        private ArmorData _equippedArmor;
        private AccessoryData[] _equippedAccessories;

        [Inject] private Player.IPlayerStats _playerStats;

        public WeaponData EquippedWeapon => _equippedWeapon;
        public ArmorData EquippedArmor => _equippedArmor;
        public AccessoryData[] EquippedAccessories => _equippedAccessories;
        public string SaveKey => "EquipmentSystem";

        public int TotalAttackPower
        {
            get
            {
                int total = _equippedWeapon?.AttackPower ?? 0;
                foreach (var accessory in _equippedAccessories)
                {
                    if (accessory != null)
                        total += accessory.AttackBonus;
                }
                return total;
            }
        }

        public int TotalDefense
        {
            get
            {
                int total = _equippedArmor?.Defense ?? 0;
                foreach (var accessory in _equippedAccessories)
                {
                    if (accessory != null)
                        total += accessory.DefenseBonus;
                }
                return total;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _equippedAccessories = new AccessoryData[_accessorySlotCount];
        }

        public bool Equip(EquipmentData equipment)
        {
            if (equipment == null) return false;
            if (!CanEquip(equipment)) return false;

            switch (equipment.SlotType)
            {
                case EquipmentSlotType.Weapon:
                    var weapon = equipment as WeaponData;
                    if (_equippedWeapon != null)
                        Unequip(EquipmentSlotType.Weapon);
                    _equippedWeapon = weapon;
                    break;

                case EquipmentSlotType.Armor:
                    var armor = equipment as ArmorData;
                    if (_equippedArmor != null)
                        Unequip(EquipmentSlotType.Armor);
                    _equippedArmor = armor;
                    break;

                case EquipmentSlotType.Accessory:
                    var accessory = equipment as AccessoryData;
                    int emptySlot = FindEmptyAccessorySlot();
                    if (emptySlot == -1)
                    {
                        Unequip(EquipmentSlotType.Accessory, 0);
                        emptySlot = 0;
                    }
                    _equippedAccessories[emptySlot] = accessory;
                    break;
            }

            ApplyEquipmentStats(equipment, true);

            EventBus.Publish(new EquipmentChangedEvent
            {
                Equipment = equipment,
                SlotType = equipment.SlotType,
                IsEquipped = true
            });

            Debug.Log($"[EquipmentSystem] Equipped: {equipment.EquipmentName}");
            return true;
        }

        public void Unequip(EquipmentSlotType slotType, int slotIndex = 0)
        {
            EquipmentData unequipped = null;

            switch (slotType)
            {
                case EquipmentSlotType.Weapon:
                    if (_equippedWeapon != null)
                    {
                        unequipped = _equippedWeapon;
                        ApplyEquipmentStats(_equippedWeapon, false);
                        _equippedWeapon = null;
                    }
                    break;

                case EquipmentSlotType.Armor:
                    if (_equippedArmor != null)
                    {
                        unequipped = _equippedArmor;
                        ApplyEquipmentStats(_equippedArmor, false);
                        _equippedArmor = null;
                    }
                    break;

                case EquipmentSlotType.Accessory:
                    if (slotIndex >= 0 && slotIndex < _equippedAccessories.Length && _equippedAccessories[slotIndex] != null)
                    {
                        unequipped = _equippedAccessories[slotIndex];
                        ApplyEquipmentStats(_equippedAccessories[slotIndex], false);
                        _equippedAccessories[slotIndex] = null;
                    }
                    break;
            }

            if (unequipped != null)
            {
                EventBus.Publish(new EquipmentChangedEvent
                {
                    Equipment = unequipped,
                    SlotType = slotType,
                    IsEquipped = false
                });

                Debug.Log($"[EquipmentSystem] Unequipped: {unequipped.EquipmentName}");
            }
        }

        public bool CanEquip(EquipmentData equipment)
        {
            if (equipment == null) return false;
            if (_playerStats != null && _playerStats.Level < equipment.RequiredLevel)
            {
                Debug.LogWarning($"[EquipmentSystem] Required level {equipment.RequiredLevel}");
                return false;
            }
            return true;
        }

        private int FindEmptyAccessorySlot()
        {
            for (int i = 0; i < _equippedAccessories.Length; i++)
            {
                if (_equippedAccessories[i] == null)
                    return i;
            }
            return -1;
        }

        private void ApplyEquipmentStats(EquipmentData equipment, bool apply)
        {
            if (_playerStats == null) return;

            float multiplier = apply ? 1f : -1f;

            if (equipment is WeaponData weapon)
            {
                _playerStats.AddStatBonus(Player.StatType.AttackPower, weapon.AttackPower * multiplier);
            }
            else if (equipment is ArmorData armor)
            {
                _playerStats.AddStatBonus(Player.StatType.Defense, armor.Defense * multiplier);
            }
            else if (equipment is AccessoryData accessory)
            {
                _playerStats.AddStatBonus(Player.StatType.AttackPower, accessory.AttackBonus * multiplier);
                _playerStats.AddStatBonus(Player.StatType.Defense, accessory.DefenseBonus * multiplier);
                _playerStats.AddStatBonus(Player.StatType.CriticalChance, accessory.CriticalChanceBonus * multiplier);
                _playerStats.AddStatBonus(Player.StatType.MoveSpeed, accessory.MoveSpeedBonus * multiplier);
            }
        }

        #region ISaveable

        public object GetSaveData()
        {
            return new EquipmentSaveData
            {
                WeaponId = _equippedWeapon?.EquipmentId,
                ArmorId = _equippedArmor?.EquipmentId,
                AccessoryIds = Array.ConvertAll(_equippedAccessories, a => a?.EquipmentId)
            };
        }

        public void LoadSaveData(object data)
        {
            if (data is EquipmentSaveData saveData)
            {
            }
        }

        [Serializable]
        public class EquipmentSaveData
        {
            public string WeaponId;
            public string ArmorId;
            public string[] AccessoryIds;
        }

        #endregion
    }

    public class EquipmentChangedEvent
    {
        public EquipmentData Equipment { get; set; }
        public EquipmentSlotType SlotType { get; set; }
        public bool IsEquipped { get; set; }
    }
}