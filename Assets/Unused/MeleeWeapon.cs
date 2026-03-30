using UnityEngine;

namespace SunnysideIsland.Combat
{
    /// <summary>
    /// 근접 무기
    /// </summary>
    public class MeleeWeapon : Weapon
    {
        [Header("=== Melee Settings ===")]
        [SerializeField] private float _attackArc = 90f;
        [SerializeField] private LayerMask _targetLayer;
        
        public override void Attack(Vector3 direction)
        {
            // 범위 내 적 검색
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position, 
                GetAttackRange(), 
                _targetLayer
            );
            
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    damageable.TakeDamage(BaseDamage, WeaponId);
                }
            }
        }
    }
}
