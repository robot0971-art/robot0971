using UnityEngine;

namespace SunnysideIsland.Combat
{
    /// <summary>
    /// 데미지를 받을 수 있는 인터페이스
    /// </summary>
    public interface IDamageable
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        bool IsDead { get; }
        
        void TakeDamage(int damage, string source);
        void Heal(int amount);
    }
}
