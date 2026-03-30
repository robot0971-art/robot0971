using UnityEngine;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Combat
{
    /// <summary>
    /// 무기 인터페이스
    /// </summary>
    public interface IWeapon
    {
        string WeaponId { get; }
        int BaseDamage { get; }
        float GetAttackSpeed();
        float GetAttackRange();
        
        void Attack(Vector3 direction);
        bool CanAttack();
    }

    /// <summary>
    /// 무기 기본 클래스
    /// </summary>
    public abstract class Weapon : MonoBehaviour, IWeapon
    {
        [Header("=== Weapon Data ===")]
        [SerializeField] protected WeaponData _weaponData;
        
        public string WeaponId => _weaponData?.weaponId ?? "unknown";
        public int BaseDamage => _weaponData?.attackPower ?? 10;
        
        public float GetAttackSpeed()
        {
            var speed = _weaponData?.attackSpeed ?? GameData.AttackSpeed.Normal;
            switch (speed)
            {
                case GameData.AttackSpeed.VerySlow: return 0.5f;
                case GameData.AttackSpeed.Slow: return 0.75f;
                case GameData.AttackSpeed.Normal: return 1f;
                case GameData.AttackSpeed.Fast: return 1.5f;
                case GameData.AttackSpeed.VeryFast: return 2f;
                default: return 1f;
            }
        }
        
        public float GetAttackRange()
        {
            var range = _weaponData?.rangeType ?? GameData.RangeType.Melee;
            switch (range)
            {
                case GameData.RangeType.Melee: return 1f;
                case GameData.RangeType.MidRange: return 3f;
                case GameData.RangeType.LongRange: return 6f;
                default: return 1f;
            }
        }
        
        public abstract void Attack(Vector3 direction);
        
        public virtual bool CanAttack()
        {
            return true;
        }
    }
}
