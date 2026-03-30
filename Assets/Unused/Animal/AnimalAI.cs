using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Combat;
using SunnysideIsland.GameData;
using SunnysideIsland.Events;

namespace SunnysideIsland.Animal
{
    public enum AnimalState
    {
        Idle,
        Wander,
        Flee,
        Chase,
        Attack,
        Dead
    }

    public interface IAnimalAI
    {
        AnimalState CurrentState { get; }
        AnimalAIType AIType { get; }
        void TakeDamage(int damage, string source);
    }

    public abstract class AnimalAI : MonoBehaviour, IAnimalAI, IDamageable
    {
        [Header("=== Animal Data ===")]
        [SerializeField] protected AnimalData _animalData;

        [Header("=== Detection ===")]
        [SerializeField] protected float _detectionRange = 5f;
        [SerializeField] protected float _fleeRange = 10f;
        [SerializeField] protected LayerMask _playerLayer;

        [Header("=== Movement ===")]
        [SerializeField] protected float _wanderRadius = 5f;
        [SerializeField] protected float _wanderInterval = 3f;

        protected AnimalState _currentState = AnimalState.Idle;
        protected Transform _target;
        protected int _currentHealth;
        protected Vector3 _spawnPosition;
        protected Vector3 _wanderTarget;
        protected float _wanderTimer;
        protected float _moveSpeed;
        protected float _lastAttackTime;
        protected float _attackCooldown = 1.5f;

        public AnimalState CurrentState => _currentState;
        public AnimalAIType AIType => _animalData?.aiType ?? AnimalAIType.Flee;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _animalData?.hp ?? 50;
        public bool IsDead => _currentHealth <= 0;

        protected virtual void Start()
        {
            _spawnPosition = transform.position;
            _currentHealth = MaxHealth;
            _moveSpeed = GetSpeedFromData();
            ChangeState(AnimalState.Idle);
        }

        protected virtual void Update()
        {
            if (IsDead) return;

            UpdateState();
        }

        protected abstract void UpdateState();

        protected virtual float GetSpeedFromData()
        {
            return _animalData?.speed switch
            {
                AnimalSpeed.Slow => 1.5f,
                AnimalSpeed.Normal => 3f,
                AnimalSpeed.Fast => 5f,
                AnimalSpeed.VeryFast => 7f,
                _ => 3f
            };
        }

        public virtual void ChangeState(AnimalState newState)
        {
            if (_currentState == newState) return;

            OnExitState(_currentState);
            _currentState = newState;
            OnEnterState(newState);
        }

        protected virtual void OnEnterState(AnimalState state)
        {
            switch (state)
            {
                case AnimalState.Idle:
                    _wanderTimer = Random.Range(0f, _wanderInterval);
                    break;
                case AnimalState.Wander:
                    SetRandomWanderTarget();
                    break;
            }
        }

        protected virtual void OnExitState(AnimalState state) { }

        protected virtual void UpdateIdle()
        {
            _wanderTimer -= Time.deltaTime;
            if (_wanderTimer <= 0f)
            {
                ChangeState(AnimalState.Wander);
            }

            if (DetectPlayer())
            {
                OnPlayerDetected();
            }
        }

        protected virtual void UpdateWander()
        {
            if (DetectPlayer())
            {
                OnPlayerDetected();
                return;
            }

            Vector3 direction = (_wanderTarget - transform.position);
            direction.y = 0f;

            if (direction.magnitude < 0.5f)
            {
                ChangeState(AnimalState.Idle);
                return;
            }

            transform.position += direction.normalized * _moveSpeed * 0.5f * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        protected virtual void UpdateFlee()
        {
            if (_target == null)
            {
                ChangeState(AnimalState.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.position);
            if (distance > _fleeRange)
            {
                _target = null;
                ChangeState(AnimalState.Idle);
                return;
            }

            Vector3 fleeDirection = (transform.position - _target.position).normalized;
            fleeDirection.y = 0f;

            transform.position += fleeDirection * _moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fleeDirection), Time.deltaTime * 5f);
        }

        protected virtual void UpdateChase()
        {
            if (_target == null)
            {
                ChangeState(AnimalState.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.position);

            if (distance > _detectionRange * 2f)
            {
                _target = null;
                ChangeState(AnimalState.Idle);
                return;
            }

            if (distance <= 1.5f)
            {
                ChangeState(AnimalState.Attack);
                return;
            }

            Vector3 direction = (_target.position - transform.position).normalized;
            direction.y = 0f;

            transform.position += direction * _moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        protected virtual void UpdateAttack()
        {
            if (_target == null)
            {
                ChangeState(AnimalState.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.position);

            if (distance > 2f)
            {
                ChangeState(AnimalState.Chase);
                return;
            }

            if (Time.time >= _lastAttackTime + _attackCooldown)
            {
                PerformAttack();
                _lastAttackTime = Time.time;
            }
        }

        protected virtual void PerformAttack()
        {
            var damageable = _target?.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(_animalData?.attackPower ?? 5, gameObject.name);
            }
        }

        protected virtual bool DetectPlayer()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRange, _playerLayer);
            if (hits.Length > 0)
            {
                _target = hits[0].transform;
                return true;
            }
            return false;
        }

        protected virtual void OnPlayerDetected()
        {
            switch (AIType)
            {
                case AnimalAIType.Flee:
                    ChangeState(AnimalState.Flee);
                    break;
                case AnimalAIType.Evasive:
                    if (Random.value > 0.5f)
                        ChangeState(AnimalState.Flee);
                    break;
                case AnimalAIType.Hostile:
                    ChangeState(AnimalState.Chase);
                    break;
                case AnimalAIType.Territorial:
                    float dist = Vector3.Distance(transform.position, _spawnPosition);
                    if (dist < _detectionRange)
                        ChangeState(AnimalState.Chase);
                    break;
            }
        }

        protected void SetRandomWanderTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * _wanderRadius;
            _wanderTarget = _spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        public virtual void TakeDamage(int damage, string source)
        {
            if (IsDead) return;

            _currentHealth -= damage;

            EventBus.Publish(new AnimalDamagedEvent
            {
                AnimalId = _animalData?.animalId ?? "Unknown",
                Damage = damage,
                CurrentHealth = _currentHealth
            });

            if (IsDead)
            {
                Die();
            }
            else
            {
                if (!_target && !string.IsNullOrEmpty(source))
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                        _target = player.transform;
                }
                OnPlayerDetected();
            }
        }

        public virtual void Heal(int amount)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
        }

        public virtual void Die()
        {
            ChangeState(AnimalState.Dead);

            DropLoot();

            EventBus.Publish(new AnimalDiedEvent
            {
                AnimalId = _animalData?.animalId ?? "Unknown",
                Position = transform.position
            });

            Destroy(gameObject, 2f);
        }

        protected virtual void DropLoot()
        {
            Debug.Log($"[AnimalAI] {_animalData?.animalName} dropped: {_animalData?.dropItems}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _fleeRange);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_spawnPosition, _wanderRadius);
        }
    }

    public class AnimalDamagedEvent
    {
        public string AnimalId { get; set; }
        public int Damage { get; set; }
        public int CurrentHealth { get; set; }
    }

    public class AnimalDiedEvent
    {
        public string AnimalId { get; set; }
        public Vector3 Position { get; set; }
    }
}