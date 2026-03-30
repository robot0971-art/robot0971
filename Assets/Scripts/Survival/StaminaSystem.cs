using System;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.Survival
{
    /// <summary>
    /// 스태미나 시스템 인터페이스
    /// </summary>
    public interface IStaminaSystem
    {
        float CurrentValue { get; }
        float MaxValue { get; }
        float Percentage { get; }
        
        bool TryConsume(float amount);
        void Consume(float amount);
        void StopConsumption();
        void Reset();
    }

    /// <summary>
    /// 플레이어의 스태미나를 관리하는 시스템
    /// </summary>
    public class StaminaSystem : MonoBehaviour, IStaminaSystem, ISaveable
    {
        [Header("=== Settings ===")]
        [SerializeField] private float _maxStamina = 100f;
        [SerializeField] private float _regenRate = 30f;
        [SerializeField] private float _regenDelay = 0.5f;
        
        private float _currentStamina;
        private float _timeSinceLastConsumption;
        private bool _isConsuming;
        
        public float CurrentValue => _currentStamina;
        public float MaxValue => _maxStamina;
        public float Percentage => _currentStamina / _maxStamina;
        
        public string SaveKey => "StaminaSystem";
        
        private void Awake()
        {
            _currentStamina = _maxStamina;
        }
        
        private void Update()
        {
            if (_isConsuming)
            {
                _timeSinceLastConsumption = 0f;
            }
            else
            {
                _timeSinceLastConsumption += Time.deltaTime;
                if (_timeSinceLastConsumption >= _regenDelay)
                {
                    Regenerate();
                }
            }
            
            _isConsuming = false;
        }
        
        public bool TryConsume(float amount)
        {
            if (_currentStamina < amount) return false;
            
            _currentStamina -= amount;
            _isConsuming = true;
            PublishChangedEvent(-amount);
            return true;
        }
        
        public void Consume(float amount)
        {
            _currentStamina = Mathf.Max(0, _currentStamina - amount);
            _isConsuming = true;
            PublishChangedEvent(-amount);
        }
        
        public void StopConsumption()
        {
            _isConsuming = false;
        }
        
        public void Reset()
        {
            _currentStamina = _maxStamina;
            PublishChangedEvent(0);
        }
        
        private void Regenerate()
        {
            if (_currentStamina < _maxStamina)
            {
                float oldValue = _currentStamina;
                _currentStamina = Mathf.Min(_maxStamina, _currentStamina + _regenRate * Time.deltaTime);
                PublishChangedEvent(_currentStamina - oldValue);
            }
        }
        
        private void PublishChangedEvent(float changeAmount)
        {
            EventBus.Publish(new StaminaChangedEvent
            {
                CurrentStamina = _currentStamina,
                MaxStamina = _maxStamina,
                ChangeAmount = changeAmount
            });
        }
        
        public object GetSaveData()
        {
            return new StaminaSaveData
            {
                CurrentStamina = _currentStamina
            };
        }
        
        public void LoadSaveData(object state)
        {
            if (state is StaminaSaveData data)
            {
                _currentStamina = data.CurrentStamina;
                PublishChangedEvent(0);
            }
        }
    }
    
    [Serializable]
    public class StaminaSaveData
    {
        public float CurrentStamina;
    }
}
