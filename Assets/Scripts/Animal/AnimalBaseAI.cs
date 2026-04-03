using UnityEngine;
using UnityEngine.Tilemaps;
using SunnysideIsland.Events;
using SunnysideIsland.Weather;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Animal
{
    public enum AnimalState
    {
        Idle,
        Wander,
        Flee
    }

    public abstract class AnimalBaseAI : MonoBehaviour
    {
        [Header("=== Movement Settings ===")]
        [SerializeField] protected float _moveSpeed = 2f;
        [SerializeField] protected float _wanderRadius = 15f;
        [SerializeField] protected float _wanderInterval = 3f;
        [SerializeField] protected float _idleTime = 2f;
        
        [Header("=== Flee Settings ===")]
        [SerializeField] protected float _fleeRange = 3f;
        [SerializeField] protected float _fleeSpeed = 4f;
        [SerializeField] protected LayerMask _playerLayer;
        
        [Header("=== Ground Check ===")]
        [SerializeField] protected LayerMask _groundLayer;
        
        protected Tilemap _groundTilemap;
        
        [Header("=== Breeding Settings ===")]
        [SerializeField] protected bool _isBaby = false;
        [SerializeField] protected float _babyScale = 0.5f;
        [SerializeField] protected float _growthDuration = 120f;
        [SerializeField] protected float _breedInterval = 60f;
        [SerializeField] [Range(0f, 1f)] protected float _breedChance = 0.3f;
        [SerializeField] protected int _maxBabiesPerArea = 3;
        [SerializeField] protected float _babySpeedMultiplier = 0.8f;
        
        protected float _growthTimer;
        protected float _breedTimer;
        protected float _originalMoveSpeed;
        protected Vector3 _originalScale;
        
        protected bool CanBreed => !_isBaby && _breedTimer <= 0f;
        public bool IsBaby => _isBaby;
        public bool CanProvideHarvestProducts => !_isBaby;
        
        [Header("=== Weather Settings ===")]
        [SerializeField] protected float _rainSpeedMultiplier = 0.8f;
        protected bool _isRaining = false;
        
        [Header("=== Components ===")]
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected Animator _animator;
        
        protected Vector3 _spawnPosition;
        protected Vector3 _wanderTarget;
        protected float _wanderTimer;
        protected float _idleTimer;
        protected Transform _playerTransform;
        protected AnimalState _currentState = AnimalState.Idle;
        
        protected virtual void Awake()
        {
            Debug.Log($"[AnimalBaseAI] Awake called on {gameObject.name}");
            _spawnPosition = transform.position;
            _originalScale = transform.localScale;
            // _originalMoveSpeed는 Start에서 설정 (하위 클래스 Awake보다 나중에)
            
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_playerLayer == 0)
                _playerLayer = LayerMask.GetMask("Player");
            
            // Ground Tilemap 찾기
            var groundObj = GameObject.Find("Grass");
            if (groundObj != null)
                _groundTilemap = groundObj.GetComponent<Tilemap>();
            
            // 새끼 초기화
            InitializeBaby();
        }
        
        public virtual void InitializeBaby()
        {
            if (_isBaby)
            {
                _growthTimer = _growthDuration;
                transform.localScale = _originalScale * _babyScale;
                _moveSpeed = _originalMoveSpeed * _babySpeedMultiplier;
                Debug.Log($"[AnimalBaseAI] {gameObject.name} initialized as baby");
            }
        }
        
        protected virtual void Start()
        {
            Debug.Log($"[AnimalBaseAI] Start called on {gameObject.name}");
            _spawnPosition = transform.position;
            _idleTimer = _idleTime;
            
            // 원래 속도 저장 (하위 클래스 Awake 이후)
            _originalMoveSpeed = _moveSpeed;
            
            // 플레이어 찾기
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
            
            // 날씨 이벤트 구독
            EventBus.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
            
            // 현재 날씨 확인
            CheckCurrentWeather();
                
            Debug.Log($"[AnimalBaseAI] {gameObject.name} - Player found: {_playerTransform != null}");
        }
        
        protected virtual void OnDestroy()
        {
            EventBus.Unsubscribe<WeatherChangedEvent>(OnWeatherChanged);
        }
        
        private void OnWeatherChanged(WeatherChangedEvent evt)
        {
            UpdateWeatherEffect(evt.CurrentWeather);
        }
        
        private void CheckCurrentWeather()
        {
            // 현재 씬에서 WeatherSystem 찾기
            var weatherSystem = FindObjectOfType<WeatherSystem>();
            if (weatherSystem != null)
            {
                UpdateWeatherEffect(weatherSystem.CurrentWeather);
            }
        }
        
        private void UpdateWeatherEffect(WeatherType weather)
        {
            bool wasRaining = _isRaining;
            _isRaining = weather == WeatherType.Rainy || weather == WeatherType.Stormy;
            
            if (_isRaining && !wasRaining)
            {
                // 비 시작 - 속도 감소
                _moveSpeed = _originalMoveSpeed * _rainSpeedMultiplier;
                Debug.Log($"[AnimalBaseAI] {gameObject.name} - Raining, speed reduced to {_moveSpeed}");
            }
            else if (!_isRaining && wasRaining)
            {
                // 비 종료 - 속도 복원
                _moveSpeed = _originalMoveSpeed;
                Debug.Log($"[AnimalBaseAI] {gameObject.name} - Rain stopped, speed restored to {_moveSpeed}");
            }
        }
        
        protected virtual void Update()
        {
            // Grass 밖에 있으면 즉시 Grass로 돌아가기
            if (!IsOnGround(transform.position))
            {
                Debug.Log($"[AnimalBaseAI] {gameObject.name} not on ground, returning...");
                ReturnToGround();
                return;
            }
            
            // 성장 처리
            UpdateGrowth();
            
            // 번식 타이머 업데이트
            if (_breedTimer > 0f)
            {
                _breedTimer -= Time.deltaTime;
            }
            
            // 하위 클래스에서 추가 로직 실행
            UpdateState();
            
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
        }
        
        protected virtual void UpdateGrowth()
        {
            if (_isBaby && _growthTimer > 0f)
            {
                _growthTimer -= Time.deltaTime;
                if (_growthTimer <= 0f)
                {
                    GrowUp();
                }
            }
        }
        
        protected virtual void GrowUp()
        {
            _isBaby = false;
            transform.localScale = _originalScale;
            _moveSpeed = _originalMoveSpeed;
            Debug.Log($"[AnimalBaseAI] {gameObject.name} has grown up!");
        }
        
        protected virtual void UpdateState()
        {
            // 하위 클래스에서 오버라이드하여 추가 로직 구현
        }
        
        protected virtual void UpdateIdle()
        {
            _idleTimer -= Time.deltaTime;
            
            if (DetectPlayer())
            {
                ChangeState(AnimalState.Flee);
                return;
            }
            
            // 번식 시도
            TryBreed();
            
            if (_idleTimer <= 0f)
            {
                ChangeState(AnimalState.Wander);
            }
        }
        
        protected virtual void UpdateWander()
        {
            if (DetectPlayer())
            {
                ChangeState(AnimalState.Flee);
                return;
            }
            
            // 목표가 Grass 밖이면 새 목표 설정
            if (!IsOnGround(_wanderTarget))
            {
                SetRandomWanderTargetOnGround();
                if (_wanderTarget == Vector3.zero)
                {
                    ChangeState(AnimalState.Idle);
                    return;
                }
            }
            
            // 목표에 도달했으면 Idle로
            if (Vector3.Distance(transform.position, _wanderTarget) < 0.3f)
            {
                ChangeState(AnimalState.Idle);
                return;
            }
            
            // 이동
            Vector3 direction = (_wanderTarget - transform.position).normalized;
            Vector3 nextPosition = transform.position + direction * _moveSpeed * Time.deltaTime;
            
            // 다음 위치가 Grass 위인지 확인
            if (!IsOnGround(nextPosition))
            {
                SetRandomWanderTargetOnGround();
                return;
            }
            
            transform.position = nextPosition;
            
            // 스프라이트 방향
            if (_spriteRenderer != null && direction.x != 0)
            {
                _spriteRenderer.flipX = direction.x > 0;
            }
        }
        
        protected virtual void UpdateFlee()
        {
            if (_playerTransform == null)
            {
                ChangeState(AnimalState.Idle);
                return;
            }
            
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            
            if (distance > _fleeRange * 1.5f)
            {
                ChangeState(AnimalState.Idle);
                return;
            }
            
            Vector3 fleeDirection = (transform.position - _playerTransform.position).normalized;
            Vector3 nextPosition = transform.position + fleeDirection * _fleeSpeed * Time.deltaTime;
            
            if (!IsOnGround(nextPosition))
            {
                ChangeState(AnimalState.Idle);
                return;
            }
            
            transform.position = nextPosition;
            
            if (_spriteRenderer != null && fleeDirection.x != 0)
            {
                _spriteRenderer.flipX = fleeDirection.x > 0;
            }
        }
        
        protected virtual void ChangeState(AnimalState newState)
        {
            _currentState = newState;
            
            switch (newState)
            {
                case AnimalState.Idle:
                    _idleTimer = Random.Range(_idleTime * 0.5f, _idleTime * 1.5f);
                    UpdateAnimatorState(0);
                    break;
                case AnimalState.Wander:
                    SetRandomWanderTargetOnGround();
                    UpdateAnimatorState(1);
                    break;
                case AnimalState.Flee:
                    UpdateAnimatorState(1);
                    break;
            }
            
            OnEnterState(newState);
        }
        
        protected virtual void OnEnterState(AnimalState state)
        {
            // 하위 클래스에서 오버라이드하여 상태 진입 시 추가 로직 구현
        }
        
        protected virtual void SetRandomWanderTargetOnGround()
        {
            for (int i = 0; i < 30; i++)
            {
                float randomX = (Random.value * 2f - 1f) * _wanderRadius;
                float randomY = (Random.value * 2f - 1f) * _wanderRadius;
                Vector3 candidatePos = _spawnPosition + new Vector3(randomX, randomY, 0);
                
                if (IsOnGround(candidatePos))
                {
                    _wanderTarget = candidatePos;
                    return;
                }
            }
            
            _wanderTarget = Vector3.zero;
        }
        
        protected virtual void ReturnToGround()
        {
            for (float radius = 1f; radius <= 10f; radius += 1f)
            {
                for (int angle = 0; angle < 360; angle += 30)
                {
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 checkPos = transform.position + new Vector3(
                        Mathf.Cos(rad) * radius, 
                        Mathf.Sin(rad) * radius, 
                        0
                    );
                    
                    if (IsOnGround(checkPos))
                    {
                        transform.position = Vector3.MoveTowards(
                            transform.position, 
                            checkPos, 
                            _moveSpeed * 2f * Time.deltaTime
                        );
                        return;
                    }
                }
            }
        }
        
        protected virtual bool IsOnGround(Vector3 position)
        {
            // Tilemap으로 직접 체크 (Collider 없이도 작동)
            if (_groundTilemap != null)
            {
                var cellPos = _groundTilemap.WorldToCell(position);
                return _groundTilemap.GetTile(cellPos) != null;
            }
            
            // 폴백: LayerMask 기반 체크
            if (_groundLayer == 0) 
            {
                return true;
            }
            
            return Physics2D.OverlapCircle(position, 0.3f, _groundLayer) != null;
        }
        
        protected virtual bool DetectPlayer()
        {
            if (_playerTransform == null) return false;
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            return distance < _fleeRange;
        }
        
        protected virtual void TryBreed()
        {
            if (!CanBreed) return;
            
            // 번식 확률 체크
            if (Random.value > _breedChance) return;
            
            // 지역당 새끼 수 제한 체크
            int babyCount = CountBabiesInArea();
            if (babyCount >= _maxBabiesPerArea) return;
            
            // 새끼 생성
            SpawnBaby();
            
            // 번식 쿨타임 설정
            _breedTimer = _breedInterval;
        }
        
        protected virtual int CountBabiesInArea()
        {
            var animals = GameObject.FindObjectsOfType<AnimalBaseAI>();
            int count = 0;
            foreach (var animal in animals)
            {
                if (animal._isBaby && Vector3.Distance(transform.position, animal.transform.position) < _wanderRadius * 2f)
                {
                    count++;
                }
            }
            return count;
        }
        
        protected virtual void SpawnBaby()
        {
            // 부모 근처에서 Grass 위를 찾아 새끼 생성
            Vector3 spawnPos = FindValidBabySpawnPosition();
            if (spawnPos == Vector3.zero) return;
            
            var baby = Instantiate(gameObject, spawnPos, Quaternion.identity);
            baby.name = $"{gameObject.name}_Baby";
            
            var babyAI = baby.GetComponent<AnimalBaseAI>();
            if (babyAI != null)
            {
                babyAI._isBaby = true;
                babyAI.InitializeBaby();
            }
            
            Debug.Log($"[AnimalBaseAI] {gameObject.name} spawned a baby at {spawnPos}");
        }
        
        protected virtual Vector3 FindValidBabySpawnPosition()
        {
            for (int i = 0; i < 20; i++)
            {
                float angle = Random.value * 360f * Mathf.Deg2Rad;
                float distance = Random.Range(1f, 3f);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
                Vector3 candidatePos = transform.position + offset;
                
                if (IsOnGround(candidatePos))
                {
                    return candidatePos;
                }
            }
            return Vector3.zero;
        }
        
        protected virtual void UpdateAnimatorState(int state)
        {
            if (_animator != null)
            {
                _animator.SetInteger("State", state);
            }
        }
        
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _wanderRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _fleeRange);
        }
    }
}
