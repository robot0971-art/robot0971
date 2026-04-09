using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Animal
{
    public enum ChickenColor
    {
        White,  // 암컷 - 계란 생산 가능
        Black   // 다른 색 - 계란 생산 없음
    }


    public class ChickenAI : AnimalBaseAI
    {
        [Header("=== Chicken Settings ===")]
        [SerializeField] private ChickenColor _chickenColor = ChickenColor.White;
        [SerializeField] private float _eggPointSearchRadius = 15f;
        [SerializeField] private float _layEggDuration = 2f;
        [SerializeField] private LayerMask _eggPointLayer;
        private LayerMask _grassLayer;

        // 닭의 움직임 설정 (AnimalAI 기본값 오버라이드)

        [Header("=== Movement Override ===")]
        [SerializeField] private float _chickenWanderRadius = 25f;  // 더 넓게 (기본 5f)
        [SerializeField] private float _chickenWanderInterval = 1.0f;  // 더 자주 (기본 3f)


        private EggPoint _targetEggPoint;
        private float _layEggTimer;
        private bool _isLayingEgg;
        private static List<EggPoint> _allEggPoints = new List<EggPoint>();
        private UnityEngine.Tilemaps.Tilemap _grassTilemap;
        private Transform _target;

        public ChickenColor ChickenColor => _chickenColor;
        public bool CanLayEggs => _chickenColor == ChickenColor.White && !_isBaby;


        protected override void Awake()
        {
            base.Awake();
            _grassLayer = LayerMask.GetMask("Grass", "Ground");
            EnsureInitialized();
        }


        private void EnsureInitialized()
        {
            if (_moveSpeed <= 0f)
            {
                _moveSpeed = 3f;
            }


            if (_spawnPosition == Vector3.zero)
            {
                _spawnPosition = transform.position;
            }

            // 닭의 움직임 설정 적용

            _wanderRadius = _chickenWanderRadius;
            _wanderInterval = _chickenWanderInterval;
        }


        protected override void Start()
        {
            base.Start();


            EnsureInitialized();


            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();


            if (_animator == null)
                _animator = GetComponent<Animator>();


            if (_eggPointLayer == 0)
                _eggPointLayer = LayerMask.GetMask("EggPoint");

            // Ground/Grass 레이어가 설정되지 않았으면 기본값 사용
            if (_grassLayer == 0)
                _grassLayer = LayerMask.GetMask("Grass", "Ground");

            // Grass Tilemap 미리 찾기
            var grassObj = GameObject.Find("Grass");
            if (grassObj != null)
                _grassTilemap = grassObj.GetComponent<UnityEngine.Tilemaps.Tilemap>();

            FindAllEggPoints();
        }


        private static float _globalSearchTimer = 0f;

        private void FindAllEggPoints()
        {
            _allEggPoints.Clear();
            _allEggPoints.AddRange(FindObjectsOfType<EggPoint>());
            _globalSearchTimer = Time.time + 2f;
        }


        protected override void UpdateState()
        {
            if (_isLayingEgg)
            {
                UpdateLayEgg();
                return;
            }

            switch (_currentState)
            {
                case AnimalState.Idle:
                    UpdateIdle();
                    break;
                case AnimalState.Wander:
                    UpdateWander();
                    break;
                case AnimalState.Flee:
                    UpdateFlee();
                    break;
            }

            // 간단하게: 이동 중 무작위로 알 낳기 (EggPoint 불필요)
            if (CanLayEggs && _currentState == AnimalState.Wander && !_isLayingEgg)
            {
                TryLayEggRandomly();
            }
        }


        protected override void UpdateIdle()
        {
            _wanderTimer -= Time.deltaTime;

            if (_wanderTimer <= 0f)
            {
                ChangeState(AnimalState.Wander);
            }
        }


        protected override void UpdateWander()
        {
            // 목표가 Grass 밖으로 나가면 즉시 새 목표 설정 (Sea 감지)
            if (_wanderTarget != Vector3.zero && _grassLayer != 0 && !IsOnGrass(_wanderTarget))
            {
                SetRandomWanderTarget();
                if (_wanderTarget == Vector3.zero)
                {
                    ChangeState(AnimalState.Idle);
                    return;
                }
            }

            // _wanderTarget이 설정되지 않았거나 Grass 밖에 있으면 새로 설정

            if (_wanderTarget == Vector3.zero || (_grassLayer != 0 && !IsOnGrass(_wanderTarget)))
            {
                SetRandomWanderTarget();
                // Grass 위를 찾지 못했으면 Idle로 전환
                if (_wanderTarget == Vector3.zero)
                {
                    ChangeState(AnimalState.Idle);
                    return;
                }
            }

            // 현재 위치가 Grass 밖이면 즉시 Grass 안쪽으로 새 목표 설정

            if (_grassLayer != 0 && !IsOnGrass(transform.position))
            {
                SetRandomWanderTarget();
                if (_wanderTarget == Vector3.zero)
                {
                    ChangeState(AnimalState.Idle);
                    return;
                }
            }


            if (_targetEggPoint != null)
            {
                // EggPoint로 이동 (Grass 위에 있는지 확인)
                if (_grassLayer != 0 && !IsOnGrass(_targetEggPoint.transform.position))
                {
                    // EggPoint가 Grass 밖에 있으면 포기
                    _targetEggPoint = null;
                    SetRandomWanderTarget();
                    return;
                }
                MoveToPosition(_targetEggPoint.transform.position);


                if (_targetEggPoint != null && Vector3.Distance(transform.position, _targetEggPoint.transform.position) < 0.5f)
                {
                    StartLayEgg();
                }
            }
            else
            {
                if (DetectPlayer())
                {
                    OnPlayerDetected();
                    return;
                }

                Vector3 direction = (_wanderTarget - transform.position);

                if (direction.magnitude < 0.5f)
                {
                    ChangeState(AnimalState.Idle);
                    return;
                }

                // 다음 위치가 Grass 위인지 미리 체크
                Vector3 nextPosition = transform.position + direction.normalized * _moveSpeed * Time.deltaTime;
                if (_grassLayer != 0 && !IsOnGrass(nextPosition))
                {
                    // Grass 밖으로 나가려 하면 새 목표 설정
                    SetRandomWanderTarget();
                    return;
                }

                Vector3 moveDirection = direction.normalized;
                transform.position += moveDirection * _moveSpeed * Time.deltaTime;


                if (_spriteRenderer != null && moveDirection.x != 0)
                {
                    _spriteRenderer.flipX = moveDirection.x > 0;
                }
            }
        }

        private void OnPlayerDetected()
        {
            _target = _playerTransform;
            ChangeState(AnimalState.Flee);
        }

        protected override void UpdateFlee()
        {
            if (_target == null)
            {
                ChangeState(AnimalState.Idle);
                _targetEggPoint = null;
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.position);
            if (distance > _fleeRange)
            {
                _target = null;
                _targetEggPoint = null;
                ChangeState(AnimalState.Idle);
                return;
            }

            Vector3 fleeDirection = (transform.position - _target.position).normalized;

            // 도망갈 위치가 Grass 밖인지 체크
            Vector3 nextPosition = transform.position + fleeDirection * _moveSpeed * Time.deltaTime;
            if (_grassLayer != 0 && !IsOnGrass(nextPosition))
            {
                // Grass 밖으로 도망가려 하면 Idle로 전환 (도망 포기)
                _target = null;
                _targetEggPoint = null;
                ChangeState(AnimalState.Idle);
                return;
            }

            transform.position += fleeDirection * _moveSpeed * Time.deltaTime;


            if (_spriteRenderer != null && fleeDirection.x != 0)
            {
                _spriteRenderer.flipX = fleeDirection.x > 0;
            }


            _targetEggPoint = null;
        }


        private void UpdateLayEgg()
        {
            _layEggTimer -= Time.deltaTime;
            if (_layEggTimer <= 0f)
            {
                FinishLayEgg();
            }
        }

        private float _eggLayTimer = 0f;
        private readonly float _eggLayInterval = 20f;  // 20초마다 알 시도
        private readonly float _eggLayChance = 0.5f;   // 50% 확률로 알 낳기

        private void TryLayEggRandomly()
        {
            _eggLayTimer -= Time.deltaTime;
            if (_eggLayTimer > 0f) return;

            // Grass 위에서만 알 낳기
            if (!IsOnGrass(transform.position)) 
            {
                _eggLayTimer = _eggLayInterval;  // 다음 시도를 위해 타이머 리셋
                return;
            }

            _eggLayTimer = _eggLayInterval;

            if (Random.value <= _eggLayChance)
            {
                SpawnEggAtCurrentPosition();
            }
            else
            {
            }
        }

        private void SpawnEggAtCurrentPosition()
        {
            var eggPrefab = Resources.Load<GameObject>("Prefabs/Egg");
            if (eggPrefab != null)
            {
                var egg = Instantiate(eggPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"[ChickenAI] {name} Egg prefab not found!");
            }
        }


        private void StartLayEgg()
        {
            _isLayingEgg = true;
            _layEggTimer = _layEggDuration;


            if (_animator != null)
            {
                _animator.SetInteger("State", 2);
            }
        }


        private void FinishLayEgg()
        {
            _isLayingEgg = false;
            _targetEggPoint = null;
            _wanderTimer = 0f;  // 즉시 Wander로 전환
            ChangeState(AnimalState.Wander);
        }


        protected override void OnEnterState(AnimalState state)
        {
            base.OnEnterState(state);


            UpdateAnimatorState(state);


            switch (state)
            {
                case AnimalState.Wander:
                    if (_targetEggPoint == null)
                    {
                        SetRandomWanderTarget();
                    }
                    break;
            }
        }


        private void UpdateAnimatorState(AnimalState state)
        {
            if (_animator == null) return;


            int animatorState = 0;


            switch (state)
            {
                case AnimalState.Idle:
                    animatorState = 0;
                    break;
                case AnimalState.Wander:
                case AnimalState.Flee:
                    animatorState = 1;
                    break;
                default:
                    animatorState = 0;
                    break;
            }


            _animator.SetInteger("State", animatorState);
        }


        private bool IsOnGrass(Vector3 position)
        {
            if (_grassLayer == 0) return true; // Grass 레이어가 없으면 항상 true

            // Tilemap 캐싱

            if (_grassTilemap == null)
            {
                var grassObj = GameObject.Find("Grass");
                if (grassObj != null)
                    _grassTilemap = grassObj.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            }

            // Tilemap으로 직접 체크 (Collider 없이도 작동)

            if (_grassTilemap != null)
            {
                var cellPos = _grassTilemap.WorldToCell(position);
                return _grassTilemap.GetTile(cellPos) != null;
            }

            // 폴백: Physics2D 체크

            return Physics2D.OverlapPoint(position, _grassLayer) != null;
        }


        private void MoveToPosition(Vector3 target)
        {
            Vector3 direction = (target - transform.position).normalized;


            if (direction != Vector3.zero)
            {
                // 다음 위치가 Grass 위인지 체크
                Vector3 nextPosition = transform.position + direction * _moveSpeed * Time.deltaTime;
                if (_grassLayer != 0 && !IsOnGrass(nextPosition))
                {
                    // Grass 밖으로 가려 하면 멈추고 Idle로
                    ChangeState(AnimalState.Idle);
                    _targetEggPoint = null; // EggPoint 포기
                    return;
                }


                transform.position += direction * _moveSpeed * Time.deltaTime;


                if (_spriteRenderer != null)
                {
                    _spriteRenderer.flipX = direction.x > 0;
                }
            }
        }


        private void SetRandomWanderTarget()
        {
            // Grass 타일맵 범위 내에서 랜덤 위치 찾기
            if (_grassTilemap != null)
            {
                // 최대 50번 시도
                for (int i = 0; i < 50; i++)
                {
                    // x, y 각각 별도 랜덤 (-1 ~ 1 범위) * 배회반경
                    float randomX = (Random.value * 2f - 1f) * _wanderRadius;
                    float randomY = (Random.value * 2f - 1f) * _wanderRadius;
                    // 현재 위치 대신 _spawnPosition 기준으로 설정
                    Vector3 candidatePos = _spawnPosition + new Vector3(randomX, randomY, 0);

                    // Grass 위인지 확인 (Tilemap 기준)

                    if (IsOnGrass(candidatePos))
                    {
                        _wanderTarget = candidatePos;
                        return;
                    }
                }
            }

            // Grass 레이어 기반 폴백

            if (_grassLayer != 0)
            {
                for (int i = 0; i < 30; i++)
                {
                    float randomX = (Random.value * 2f - 1f) * _wanderRadius;
                    float randomY = (Random.value * 2f - 1f) * _wanderRadius;
                    // 현재 위치 대신 _spawnPosition 기준으로 설정
                    Vector3 candidatePos = _spawnPosition + new Vector3(randomX, randomY, 0);


                    if (Physics2D.OverlapPoint(candidatePos, _grassLayer) != null)
                    {
                        _wanderTarget = candidatePos;
                        return;
                    }
                }
            }

            // 실패 시 Idle로 전환하도록 zero 설정
            _wanderTarget = Vector3.zero;
        }


        public void OnDayPassed()
        {
            _allEggPoints.Clear();
            FindAllEggPoints();
        }
    }
}
