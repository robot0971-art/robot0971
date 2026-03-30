using System.Collections;
using UnityEngine;
using SunnysideIsland.Combat;
using SunnysideIsland.Events;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Enemy
{
    /// <summary>
    /// 보스 상태 (기본 EnemyState 확장)
    /// </summary>
    public enum BossState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        SpecialAttack,
        Summon,
        Enraged,
        Stunned,
        Dead
    }

    /// <summary>
    /// 보스 공격 패턴
    /// </summary>
    public enum BossAttackPattern
    {
        NormalAttack,     // 일반 공격
        HeavyAttack,      // 강한 공격
        Whirlwind,        // 회전 공격
        Charge,           // 돌진
        SummonMinions,    // 부하 소환
        GroundSlam        // 지면 강타
    }

    /// <summary>
    /// 고블린 족장 보스 AI
    /// Chapter 4의 최종 보스
    /// </summary>
    public class GoblinChiefAI : EnemyAI
    {
        [Header("=== Boss Settings ===")]
        [SerializeField] private float _heavyAttackRange = 2f;
        [SerializeField] private float _chargeSpeed = 8f;
        [SerializeField] private float _chargeDistance = 10f;
        [SerializeField] private float _whirlwindRadius = 3f;
        [SerializeField] private float _summonCooldown = 30f;
        [SerializeField] private int _maxMinions = 5;
        [SerializeField] private float _enrageHealthPercent = 0.3f;
        
        [Header("=== Boss Data ===")]
        [SerializeField] private GameObject _minionPrefab;
        [SerializeField] private Transform _summonPointsParent;
        [SerializeField] private ParticleSystem _enrageEffect;
        [SerializeField] private ParticleSystem _groundSlamEffect;
        
        private BossState _bossState = BossState.Idle;
        private BossAttackPattern _currentPattern;
        private bool _isEnraged = false;
        private bool _isAttacking = false;
        private float _lastSummonTime = -999f;
        private int _attackCombo = 0;
        private Transform[] _summonPoints;
        
        public BossState CurrentBossState => _bossState;
        public bool IsEnraged => _isEnraged;
        
        protected override void Start()
        {
            base.Start();
            
            if (_summonPointsParent != null)
            {
                _summonPoints = new Transform[_summonPointsParent.childCount];
                for (int i = 0; i < _summonPointsParent.childCount; i++)
                {
                    _summonPoints[i] = _summonPointsParent.GetChild(i);
                }
            }
            
            _bossState = BossState.Idle;
        }
        
        protected override void UpdateState()
        {
            if (_isAttacking) return;
            
            switch (_bossState)
            {
                case BossState.Idle:
                    UpdateIdle();
                    break;
                case BossState.Patrol:
                    UpdatePatrol();
                    break;
                case BossState.Chase:
                    UpdateChase();
                    break;
                case BossState.Enraged:
                    UpdateEnraged();
                    break;
                case BossState.Dead:
                    break;
            }
        }
        
        private void UpdateIdle()
        {
            if (DetectPlayer())
            {
                ChangeBossState(BossState.Chase);
            }
        }
        
        private void UpdatePatrol()
        {
            if (DetectPlayer())
            {
                ChangeBossState(BossState.Chase);
            }
        }
        
        private void UpdateChase()
        {
            if (_target == null)
            {
                ChangeBossState(BossState.Idle);
                return;
            }
            
            float distance = Vector2.Distance(transform.position, _target.position);
            
            // 체력 체크 - 격노 모드
            if (!_isEnraged && (float)_currentHealth / MaxHealth <= _enrageHealthPercent)
            {
                ActivateEnrage();
                return;
            }
            
            // 소환 가능 체크
            if (CanSummonMinions())
            {
                StartCoroutine(ExecuteSummonPattern());
                return;
            }
            
            // 공격 범위 내
            if (distance <= _attackRange)
            {
                DecideAttackPattern(distance);
            }
            else if (distance <= _chargeDistance && _isEnraged)
            {
                StartCoroutine(ExecuteChargePattern());
            }
            else
            {
                // 추적
                Vector2 direction = (_target.position - transform.position).normalized;
                transform.position += (Vector3)direction * (_monsterData?.speed == MonsterSpeed.Fast ? 4f : 3f) * Time.deltaTime;
            }
        }
        
        private void UpdateEnraged()
        {
            UpdateChase();
        }
        
        /// <summary>
        /// 격노 모드 활성화
        /// </summary>
        private void ActivateEnrage()
        {
            _isEnraged = true;
            ChangeBossState(BossState.Enraged);
            
            if (_enrageEffect != null)
            {
                _enrageEffect.Play();
            }
            
            EventBus.Publish(new BossEnragedEvent
            {
                BossId = "GoblinChief",
                BossName = "고블린 족장"
            });
            
            Debug.Log("[GoblinChiefAI] Enraged mode activated!");
        }
        
        /// <summary>
        /// 공격 패턴 결정
        /// </summary>
        private void DecideAttackPattern(float distance)
        {
            if (_isEnraged)
            {
                // 격노 모드: 랜덤 패턴
                int pattern = Random.Range(0, 4);
                switch (pattern)
                {
                    case 0:
                        StartCoroutine(ExecuteNormalAttack());
                        break;
                    case 1:
                        StartCoroutine(ExecuteHeavyAttack());
                        break;
                    case 2:
                        StartCoroutine(ExecuteWhirlwindPattern());
                        break;
                    case 3:
                        StartCoroutine(ExecuteGroundSlamPattern());
                        break;
                }
            }
            else
            {
                // 일반 모드: 기본 공격 중심
                if (distance <= _heavyAttackRange && Random.value > 0.7f)
                {
                    StartCoroutine(ExecuteHeavyAttack());
                }
                else
                {
                    StartCoroutine(ExecuteNormalAttack());
                }
            }
        }
        
        /// <summary>
        /// 부하 소환 가능 여부
        /// </summary>
        private bool CanSummonMinions()
        {
            if (Time.time - _lastSummonTime < _summonCooldown) return false;
            if (_minionPrefab == null) return false;
            
            // 현재 소환된 부하 수 체크
            GameObject[] minions = GameObject.FindGameObjectsWithTag("Enemy");
            int minionCount = 0;
            foreach (var minion in minions)
            {
                if (minion != gameObject && minion.name.Contains("Goblin"))
                    minionCount++;
            }
            
            return minionCount < _maxMinions;
        }
        
        #region Attack Patterns
        
        private IEnumerator ExecuteNormalAttack()
        {
            _isAttacking = true;
            _currentPattern = BossAttackPattern.NormalAttack;
            
            // 공격 애니메이션/이펙트
            yield return new WaitForSeconds(0.5f);
            
            if (_target != null)
            {
                float distance = Vector2.Distance(transform.position, _target.position);
                if (distance <= _attackRange)
                {
                    // 데미지 적용
                    var damageable = _target.GetComponent<IDamageable>();
                    damageable?.TakeDamage(_monsterData?.attackPower ?? 15, "GoblinChief");
                }
            }
            
            _attackCombo++;
            if (_attackCombo >= 3)
            {
                _attackCombo = 0;
                yield return new WaitForSeconds(1f);
            }
            
            _isAttacking = false;
        }
        
        private IEnumerator ExecuteHeavyAttack()
        {
            _isAttacking = true;
            _currentPattern = BossAttackPattern.HeavyAttack;
            
            // 차징 애니메이션
            yield return new WaitForSeconds(1f);
            
            if (_target != null)
            {
                float distance = Vector2.Distance(transform.position, _target.position);
                if (distance <= _heavyAttackRange)
                {
                    var damageable = _target.GetComponent<IDamageable>();
                    damageable?.TakeDamage((_monsterData?.attackPower ?? 15) * 2, "GoblinChief");
                }
            }
            
            yield return new WaitForSeconds(1.5f);
            _isAttacking = false;
        }
        
        private IEnumerator ExecuteWhirlwindPattern()
        {
            _isAttacking = true;
            _currentPattern = BossAttackPattern.Whirlwind;
            
            // 회전 공격
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                // 범위 내 모든 적에게 데미지
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _whirlwindRadius);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        var damageable = hit.GetComponent<IDamageable>();
                        damageable?.TakeDamage((_monsterData?.attackPower ?? 15) / 2, "GoblinChief");
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(1f);
            _isAttacking = false;
        }
        
        private IEnumerator ExecuteChargePattern()
        {
            _isAttacking = true;
            _currentPattern = BossAttackPattern.Charge;
            
            Vector2 direction = (_target.position - transform.position).normalized;
            Vector2 startPos = transform.position;
            Vector2 targetPos = startPos + direction * _chargeDistance;
            
            // 돌진
            float elapsed = 0f;
            float chargeTime = _chargeDistance / _chargeSpeed;
            
            while (elapsed < chargeTime)
            {
                transform.position = Vector2.Lerp(startPos, targetPos, elapsed / chargeTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 돌진 후 스턴
            yield return new WaitForSeconds(1f);
            _isAttacking = false;
        }
        
        private IEnumerator ExecuteSummonPattern()
        {
            _isAttacking = true;
            _currentPattern = BossAttackPattern.SummonMinions;
            _lastSummonTime = Time.time;
            
            ChangeBossState(BossState.Summon);
            
            // 소환 애니메이션
            yield return new WaitForSeconds(2f);
            
            // 부하 소환
            int summonCount = Random.Range(2, 4);
            for (int i = 0; i < summonCount && _summonPoints != null; i++)
            {
                if (_summonPoints.Length > 0)
                {
                    Transform spawnPoint = _summonPoints[Random.Range(0, _summonPoints.Length)];
                    Instantiate(_minionPrefab, spawnPoint.position, Quaternion.identity);
                }
            }
            
            yield return new WaitForSeconds(1f);
            
            ChangeBossState(_isEnraged ? BossState.Enraged : BossState.Chase);
            _isAttacking = false;
        }
        
        private IEnumerator ExecuteGroundSlamPattern()
        {
            _isAttacking = true;
            _currentPattern = BossAttackPattern.GroundSlam;
            
            // 점프 후 착지
            yield return new WaitForSeconds(1f);
            
            // 지면 강타 이펙트
            if (_groundSlamEffect != null)
            {
                _groundSlamEffect.Play();
            }
            
            // 범위 데미지
            float slamRadius = 4f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    var damageable = hit.GetComponent<IDamageable>();
                    damageable?.TakeDamage((_monsterData?.attackPower ?? 15) * 3, "GoblinChief");
                }
            }
            
            yield return new WaitForSeconds(2f);
            _isAttacking = false;
        }
        
        #endregion
        
        /// <summary>
        /// 보스 상태 변경
        /// </summary>
        public void ChangeBossState(BossState newState)
        {
            if (_bossState == newState) return;
            
            _bossState = newState;
            
            // 기본 EnemyState와 동기화
            switch (newState)
            {
                case BossState.Idle:
                case BossState.Patrol:
                case BossState.Summon:
                    base.ChangeState(EnemyState.Idle);
                    break;
                case BossState.Chase:
                case BossState.Enraged:
                    base.ChangeState(EnemyState.Chase);
                    break;
                case BossState.Attack:
                case BossState.SpecialAttack:
                    base.ChangeState(EnemyState.Attack);
                    break;
                case BossState.Stunned:
                    base.ChangeState(EnemyState.Stunned);
                    break;
                case BossState.Dead:
                    base.ChangeState(EnemyState.Dead);
                    break;
            }
        }
        
        public override void TakeDamage(int damage, string source)
        {
            base.TakeDamage(damage, source);
            
            // 격노 체크
            if (!_isEnraged && (float)_currentHealth / MaxHealth <= _enrageHealthPercent)
            {
                ActivateEnrage();
            }
            
            EventBus.Publish(new BossHealthChangedEvent
            {
                BossId = "GoblinChief",
                CurrentHealth = _currentHealth,
                MaxHealth = MaxHealth
            });
        }
        
        protected override void Die()
        {
            ChangeBossState(BossState.Dead);
            
            EventBus.Publish(new BossDefeatedEvent
            {
                BossId = "GoblinChief",
                BossName = "고블린 족장",
                ExpReward = _monsterData?.expReward ?? 500
            });
            
            // 보상 드랍
            DropRewards();
            
            base.Die();
        }
        
        /// <summary>
        /// 보스 처치 보상 드랍
        /// </summary>
        private void DropRewards()
        {
            // 골드, 아이템 등 드랍 로직
            Debug.Log("[GoblinChiefAI] Boss defeated! Rewards dropped.");
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _heavyAttackRange);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _whirlwindRadius);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);
        }
    }
    
    #region Boss Events
    
    /// <summary>
    /// 보스 격노 이벤트
    /// </summary>
    public class BossEnragedEvent
    {
        public string BossId { get; set; }
        public string BossName { get; set; }
    }
    
    /// <summary>
    /// 보스 체력 변경 이벤트
    /// </summary>
    public class BossHealthChangedEvent
    {
        public string BossId { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
    }
    
    /// <summary>
    /// 보스 처치 이벤트
    /// </summary>
    public class BossDefeatedEvent
    {
        public string BossId { get; set; }
        public string BossName { get; set; }
        public int ExpReward { get; set; }
    }
    
    #endregion
}