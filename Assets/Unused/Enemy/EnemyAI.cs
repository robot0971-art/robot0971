using UnityEngine;
using SunnysideIsland.Combat;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Enemy
{
    /// <summary>
    /// 적 상태
    /// </summary>
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Stunned,
        Dead
    }

    /// <summary>
    /// 적 AI 인터페이스
    /// </summary>
    public interface IEnemyAI
    {
        EnemyState CurrentState { get; }
        float DetectionRange { get; }
        float AttackRange { get; }
        
        void ChangeState(EnemyState newState);
    }

    /// <summary>
    /// 적 AI 기본 클래스
    /// </summary>
    public abstract class EnemyAI : MonoBehaviour, IEnemyAI, IDamageable
    {
        [Header("=== AI Settings ===")]
        [SerializeField] protected MonsterData _monsterData;
        [SerializeField] protected float _detectionRange = 5f;
        [SerializeField] protected float _attackRange = 1f;
        
        protected EnemyState _currentState = EnemyState.Idle;
        protected Transform _target;
        protected int _currentHealth;
        
        public EnemyState CurrentState => _currentState;
        public float DetectionRange => _detectionRange;
        public float AttackRange => _attackRange;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _monsterData?.hp ?? 50;
        public bool IsDead => _currentHealth <= 0;
        
        protected virtual void Start()
        {
            _currentHealth = MaxHealth;
            ChangeState(EnemyState.Idle);
        }
        
        protected virtual void Update()
        {
            UpdateState();
        }
        
        protected abstract void UpdateState();
        
        public virtual void ChangeState(EnemyState newState)
        {
            if (_currentState == newState) return;
            
            OnExitState(_currentState);
            _currentState = newState;
            OnEnterState(newState);
        }
        
        protected virtual void OnEnterState(EnemyState state) { }
        protected virtual void OnExitState(EnemyState state) { }
        
        public virtual void TakeDamage(int damage, string source)
        {
            if (IsDead) return;
            
            _currentHealth -= damage;
            
            if (IsDead)
            {
                Die();
            }
            else
            {
                ChangeState(EnemyState.Chase);
            }
        }
        
        public virtual void Heal(int amount)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
        }
        
        protected virtual void Die()
        {
            ChangeState(EnemyState.Dead);
            Destroy(gameObject, 2f);
        }
        
        protected virtual bool DetectPlayer()
        {
            Collider2D playerCollider = Physics2D.OverlapCircle(
                transform.position, 
                _detectionRange, 
                LayerMask.GetMask("Player")
            );
            
            if (playerCollider != null)
            {
                _target = playerCollider.transform;
                return true;
            }
            
            return false;
        }
    }
}
