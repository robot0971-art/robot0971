using System;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace SunnysideIsland.Survival
{
    /// <summary>
    /// Tracks the player's hunger and applies starvation damage over time.
    /// </summary>
    public interface IHungerSystem
    {
        float CurrentValue { get; }
        float MaxValue { get; }
        float Percentage { get; }
        HungerState CurrentState { get; }

        void Modify(float amount);
        void SetValue(float value);
        void Reset();
    }

    /// <summary>
    /// Applies hourly hunger decay and deals health damage only for the time spent at zero hunger.
    /// </summary>
    public class HungerSystem : MonoBehaviour, IHungerSystem, ISaveable
    {
        [Header("=== Settings ===")]
        [SerializeField] private float _maxHunger = 100f;
        [SerializeField] private float _decayPerHour = 2.5f;
        [SerializeField] private int _starvationDamagePerHour = 10;

        private float _currentHunger;
        private int _lastDay = -1;
        private int _lastHour = -1;

        [Inject]
        private HealthSystem _healthSystem;

        public float CurrentValue => _currentHunger;
        public float MaxValue => _maxHunger;
        public float Percentage => _currentHunger / _maxHunger;
        public HungerState CurrentState => GetHungerState();

        public string SaveKey => "HungerSystem";

        private void Awake()
        {
            _currentHunger = _maxHunger;
        }

        private void Start()
        {
            EventBus.Subscribe<TimeChangedEvent>(OnTimeChanged);
            PublishChangedEvent();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TimeChangedEvent>(OnTimeChanged);
        }

        private void OnTimeChanged(TimeChangedEvent evt)
        {
            if (_lastDay < 0)
            {
                _lastDay = evt.Day;
                _lastHour = evt.Hour;
                return;
            }

            int dayDelta = evt.Day - _lastDay;
            int hourDelta = evt.Hour - _lastHour;

            if (dayDelta != 0 || hourDelta != 0)
            {
                float totalHours = dayDelta * 24 + hourDelta;
                if (totalHours > 0)
                {
                    float starvationHours = CalculateStarvationHours(totalHours);

                    Modify(-_decayPerHour * totalHours);

                    if (starvationHours > 0f && _healthSystem != null)
                    {
                        int damage = Mathf.RoundToInt(_starvationDamagePerHour * starvationHours);
                        if (damage > 0)
                        {
                            _healthSystem.TakeDamage(damage, "Starvation");
                        }
                    }
                }
            }

            _lastDay = evt.Day;
            _lastHour = evt.Hour;
        }

        public void Modify(float amount)
        {
            float oldValue = _currentHunger;
            _currentHunger = Mathf.Clamp(_currentHunger + amount, 0f, _maxHunger);

            if (Mathf.Abs(_currentHunger - oldValue) > 0.01f)
            {
                PublishChangedEvent();
            }
        }

        public void SetValue(float value)
        {
            _currentHunger = Mathf.Clamp(value, 0f, _maxHunger);
            PublishChangedEvent();
        }

        public void Reset()
        {
            _currentHunger = _maxHunger;
            PublishChangedEvent();
        }

        private float CalculateStarvationHours(float totalHours)
        {
            if (totalHours <= 0f || _decayPerHour <= 0f)
            {
                return 0f;
            }

            if (_currentHunger <= 0f)
            {
                return totalHours;
            }

            float hoursUntilEmpty = _currentHunger / _decayPerHour;
            return Mathf.Clamp(totalHours - hoursUntilEmpty, 0f, totalHours);
        }

        private HungerState GetHungerState()
        {
            float percentage = Percentage * 100f;

            if (percentage >= 80f)
            {
                return HungerState.Full;
            }

            if (percentage >= 40f)
            {
                return HungerState.Normal;
            }

            if (percentage >= 20f)
            {
                return HungerState.Hungry;
            }

            return HungerState.Starving;
        }

        private void PublishChangedEvent()
        {
            EventBus.Publish(new HungerChangedEvent
            {
                CurrentHunger = _currentHunger,
                MaxHunger = _maxHunger,
                State = CurrentState
            });
        }

        public object GetSaveData()
        {
            return new HungerSaveData
            {
                CurrentHunger = _currentHunger,
                LastDay = _lastDay,
                LastHour = _lastHour
            };
        }

        public void LoadSaveData(object state)
        {
            var data = state as HungerSaveData ?? (state as JObject)?.ToObject<HungerSaveData>();
            if (data != null)
            {
                _currentHunger = data.CurrentHunger;
                _lastDay = data.LastDay;
                _lastHour = data.LastHour;
                PublishChangedEvent();
            }
        }
    }

    [Serializable]
    public class HungerSaveData
    {
        public float CurrentHunger;
        public int LastDay;
        public int LastHour;
    }
}
