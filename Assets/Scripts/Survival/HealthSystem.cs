using System;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.Survival
{
    /// <summary>
    /// 체력 시스템 인터페이스
    /// </summary>
    public interface IHealthSystem
    {
        float CurrentValue { get; }
        float MaxValue { get; }
        float Percentage { get; }
        bool IsDead { get; }
        
        void TakeDamage(int amount, string source);
        void Heal(int amount, string source);
        void Reset();
    }

    /// <summary>
    /// 플레이어의 체력을 관리하는 시스템
    /// </summary>
    public class HealthSystem : MonoBehaviour, IHealthSystem, ISaveable
    {
        [Header("=== Settings ===")]
        [SerializeField] private int _maxHealth = 100;
        
        private int _currentHealth;
        
        public float CurrentValue => _currentHealth;
        public float MaxValue => _maxHealth;
        public float Percentage => (float)_currentHealth / _maxHealth;
        public bool IsDead => _currentHealth <= 0;
        
        public string SaveKey => "HealthSystem";
        
        private void Awake()
        {
            _currentHealth = _maxHealth;
        }
        
        private void Start()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            PublishChangedEvent(0);
        }
        
        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        }
        
        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            TakeDamage(evt.Damage, evt.DamageSource);
        }
        
        public void TakeDamage(int amount, string source)
        {
            if (IsDead) return;
            
            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            PublishChangedEvent(-amount);
            
            if (IsDead)
            {
                Die(source);
            }
        }
        
        public void Heal(int amount, string source)
        {
            if (IsDead) return;
            
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            PublishChangedEvent(amount);
        }
        
        public void Reset()
        {
            _currentHealth = _maxHealth;
            PublishChangedEvent(0);
        }
        
        private void Die(string reason)
        {
            EventBus.Publish(new PlayerDiedEvent
            {
                DeathReason = reason
            });
        }
        
        private void PublishChangedEvent(float changeAmount)
        {
            EventBus.Publish(new HealthChangedEvent
            {
                CurrentHealth = _currentHealth,
                MaxHealth = _maxHealth,
                ChangeAmount = changeAmount
            });
        }
        
        public object GetSaveData()
        {
            return new HealthSaveData
            {
                CurrentHealth = _currentHealth
            };
        }
        
        public void LoadSaveData(object state)
        {
            if (state is HealthSaveData data)
            {
                _currentHealth = data.CurrentHealth;
                PublishChangedEvent(0);
            }
        }
    }
    
    [Serializable]
    public class HealthSaveData
    {
        public int CurrentHealth;
    }
}
