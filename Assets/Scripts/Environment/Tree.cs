
using System;
using System.Collections;
using UnityEngine;
using SunnysideIsland.Events;
using SunnysideIsland.Core;
using SunnysideIsland.Pool;
using DI;

namespace SunnysideIsland.Environment
{
    public class Tree : MonoBehaviour, ISaveable
    {
        [Header("=== Settings ===")]
        [SerializeField] private int _maxHits = 5;
        [SerializeField] private GameObject _woodPrefab;
        [SerializeField] private int _woodAmount = 3;
        [SerializeField] private float _respawnTime = 300f; // 5분 (초 단위)

        [Header("=== Visual Effects ===")]
        [SerializeField] private float _flashDuration = 0.15f; // 반짝이는 시간
        [SerializeField] private float _shakeIntensity = 0.05f; // 흔들림 강도
        [SerializeField] private float _fallDelay = 0.4f; // 5번 히트 후 쓰러지는 딜레이 (애니메이션 시간)
        [SerializeField] private float _hitDelay = 0.15f; // 도끼가 나무에 닿는 타이밍 지연
        [SerializeField] private Vector3 _dustOffset = new Vector3(0f, 0.5f, 0f); // Dust 생성 위치 오프셋 (나무 중심에서 위로)

        // 셰이더 내부의 변수 이름 (Shader Graph에서 정한 Reference 이름과 일치해야 함)
        private static readonly int FlashAmountProp = Shader.PropertyToID("_Flash_Amount");

        private int _currentHits;
        private bool _isChopped;
        private bool _isFalling; // 벌목 중인지 여부 (딜레이 중에도 중복 방지)
        public bool IsChopped => _isChopped;
        private SpriteRenderer _spriteRenderer;
        private Material _hitMaterial;
        private Coroutine _flashCoroutine;
        private Coroutine _respawnCoroutine;
        private Collider2D[] _colliders;
        private float _respawnStartTime;
        private float _remainingRespawnTime;

        [Inject(Optional = true)] private IPoolManager _poolManager;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            // .material을 호출하면 인스턴스 복사본을 생성해 해당 나무만 반짝이게 합니다.
            if (_spriteRenderer != null) 
            {
                _hitMaterial = _spriteRenderer.material;
            }

            // 콜라이더 배열 가져오기
            _colliders = GetComponentsInChildren<Collider2D>();
        }
        
        private void Start()
        {
            DIContainer.Inject(this);
        }

        public void Chop()
        {
            if (_isChopped || _isFalling) return;

            _currentHits++;

            // 1. 비주얼 효과 실행 (반짝임 + 흔들림)
            PlayHitEffect();

            Debug.Log($"[Tree] Hit {_currentHits}/{_maxHits}");

            // 2. 벌목 완료 체크
            if (_currentHits >= _maxHits)
            {
                _isFalling = true; // 벌목 시작 플래그 설정
                Debug.Log($"[Tree] Starting FallTree coroutine with delay {_fallDelay}s");
                StartCoroutine(FallTreeDelayed());
            }
        }

        private IEnumerator FallTreeDelayed()
        {
            Debug.Log("[Tree] FallTreeDelayed coroutine started");
            yield return new WaitForSeconds(_fallDelay);
            Debug.Log("[Tree] FallTreeDelayed delay complete, calling FallTree");
            FallTree();
        }

        private void PlayHitEffect()
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(HitEffectRoutine());
        }

        private IEnumerator HitEffectRoutine()
        {
            if (_hitMaterial != null)
            {
                // 도 애니메이션 타이밍 대기 (나무에 닿는 순간까지)
                yield return new WaitForSeconds(_hitDelay);

                // [셰이더] 반짝임 켜기 (1.0)
                _hitMaterial.SetFloat(FlashAmountProp, 1.0f);

                // [쥬스] 나무 위치를 살짝 옆으로 틀어서 흔들림 표현
                Vector3 originalPos = transform.position;
                transform.position += new Vector3(UnityEngine.Random.Range(-_shakeIntensity, _shakeIntensity), 0, 0);

                yield return new WaitForSeconds(_flashDuration);

                // [셰이더] 반짝임 끄기 (0.0)
                _hitMaterial.SetFloat(FlashAmountProp, 0.0f);
                
                // [쥬스] 위치 복구
                transform.position = originalPos;
            }
        }

        private void FallTree()
        {
            Debug.Log($"[Tree] FallTree called, _poolManager={_poolManager != null}");
            _isChopped = true;
            _respawnStartTime = Time.time;
            _remainingRespawnTime = _respawnTime;

            // Dust 이펙트 재생
            if (_poolManager != null)
            {
                Vector3 dustPosition = transform.position + _dustOffset;
                Debug.Log($"[Tree] Spawning Dust at {dustPosition} (offset: {_dustOffset})");
                _poolManager.Spawn("Dust", dustPosition, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("[Tree] _poolManager is null!");
            }

            // 렌더러 비활성화
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = false;

            // 콜라이더 비활성화
            if (_colliders != null)
            {
                foreach (var col in _colliders)
                {
                    col.enabled = false;
                }
            }

            // 리스폰 코루틴 시작
            _respawnCoroutine = StartCoroutine(RespawnRoutine(_respawnTime));

            // 이벤트 버스에 벌목 알림 발행 (WoodDropManager가 처리)
            EventBus.Publish(new TreeChoppedEvent
            {
                TreePosition = transform.position,
                WoodAmount = _woodAmount
            });
            
            Debug.Log($"[Tree] Tree chopped! Spawned {_woodAmount} wood");
        }

        private void Respawn()
        {
            // 렌더러 다시 활성화
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = true;

            // 콜라이더 다시 활성화
            if (_colliders != null)
            {
                foreach (var col in _colliders)
                {
                    col.enabled = true;
                }
            }

            // 상태 초기화
            _isChopped = false;
            _currentHits = 0;

            Debug.Log("[Tree] Tree respawned");
        }
        
        private System.Collections.IEnumerator RespawnRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            Respawn();
        }

        // Save/Load implementation
        [Serializable]
        private class TreeSaveData
        {
            public int currentHits;
            public bool isChopped;
            public float remainingRespawnTime;
        }

        [SerializeField] private string _uniqueId;
        public string SaveKey => string.IsNullOrEmpty(_uniqueId) 
            ? $"Tree_{Mathf.RoundToInt(transform.position.x)}_{Mathf.RoundToInt(transform.position.y)}_{Mathf.RoundToInt(transform.position.z)}" 
            : _uniqueId;

        public object GetSaveData()
        {
            float remaining = 0f;
            if (_isChopped && _respawnCoroutine != null)
            {
                remaining = Mathf.Max(0f, _respawnTime - (Time.time - _respawnStartTime));
            }
            
            return new TreeSaveData
            {
                currentHits = _currentHits,
                isChopped = _isChopped,
                remainingRespawnTime = remaining
            };
        }

        public void LoadSaveData(object state)
        {
            if (state is TreeSaveData data)
            {
                _currentHits = data.currentHits;
                _isChopped = data.isChopped;
                
                if (_isChopped)
                {
                    // 나무가 벌목된 상태
                    if (_spriteRenderer != null)
                        _spriteRenderer.enabled = false;
                    if (_colliders != null)
                    {
                        foreach (var col in _colliders)
                            col.enabled = false;
                    }
                    
                    if (data.remainingRespawnTime > 0)
                    {
                        // 남은 시간만큼 대기 후 리스폰
                        _respawnCoroutine = StartCoroutine(RespawnRoutine(data.remainingRespawnTime));
                    }
                    else
                    {
                        // 즉시 리스폰 (이미 남은 시간이 0 이하)
                        Respawn();
                    }
                }
                else
                {
                    // 나무가 정상 상태
                    if (_spriteRenderer != null)
                        _spriteRenderer.enabled = true;
                    if (_colliders != null)
                    {
                        foreach (var col in _colliders)
                            col.enabled = true;
                    }
                }
            }
        }
    }

    // 이벤트 클래스 (같은 네임스페이스에 두어 CS0246 에러 방지)
    public class TreeChoppedEvent
    {
        public Vector3 TreePosition { get; set; }
        public int WoodAmount { get; set; }
    }
}