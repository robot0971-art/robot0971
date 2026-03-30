using UnityEngine;

namespace SunnysideIsland.Enemy
{
    /// <summary>
    /// 고블린 AI
    /// </summary>
    public class GoblinAI : EnemyAI
    {
        [Header("=== Goblin Settings ===")]
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _attackCooldown = 1f;
        
        private float _lastAttackTime;
        
        protected override void UpdateState()
        {
            switch (_currentState)
            {
                case EnemyState.Idle:
                    UpdateIdle();
                    break;
                case EnemyState.Chase:
                    UpdateChase();
                    break;
                case EnemyState.Attack:
                    UpdateAttack();
                    break;
            }
        }
        
        private void UpdateIdle()
        {
            if (DetectPlayer())
            {
                ChangeState(EnemyState.Chase);
            }
        }
        
        private void UpdateChase()
        {
            if (_target == null) return;
            
            float distance = Vector3.Distance(transform.position, _target.position);
            
            if (distance <= _attackRange)
            {
                ChangeState(EnemyState.Attack);
            }
            else if (distance > _detectionRange * 1.5f)
            {
                _target = null;
                ChangeState(EnemyState.Idle);
            }
            else
            {
                Vector3 direction = (_target.position - transform.position).normalized;
                transform.position += direction * _moveSpeed * Time.deltaTime;
            }
        }
        
        private void UpdateAttack()
        {
            if (Time.time >= _lastAttackTime + _attackCooldown)
            {
                Attack();
                _lastAttackTime = Time.time;
            }
            
            if (_target == null || Vector3.Distance(transform.position, _target.position) > _attackRange * 1.2f)
            {
                ChangeState(EnemyState.Chase);
            }
        }
        
        private void Attack()
        {
            if (_target == null) return;
            
            var damageable = _target.GetComponent<Combat.IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(_monsterData?.attackPower ?? 10, gameObject.name);
            }
        }
    }
}
