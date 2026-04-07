using DI;
using SunnysideIsland.GameData;
using SunnysideIsland.Items;
using SunnysideIsland.Pool;
using UnityEngine;
using GameDataClass = SunnysideIsland.GameData.GameData;

namespace SunnysideIsland.Animal
{
    public class PigHuntable : MonoBehaviour
    {
        [Header("=== Hunt Settings ===")]
        [SerializeField] private int _hitsToKill = 3;
        [SerializeField] private string _dropItemId = "pork";
        [SerializeField] private int _minDropAmount = 1;
        [SerializeField] private int _maxDropAmount = 2;
        [SerializeField] [Range(0f, 1f)] private float _bonusDropChance = 0.2f;
        [SerializeField] private int _bonusDropAmount = 1;

        [Header("=== Pool Settings ===")]
        [SerializeField] private string _pigPoolName = "Pig";
        [SerializeField] private string _porkPoolName = "Pork";
        [SerializeField] private string _dustPoolName = "Dust";
        [SerializeField] private int _porkPoolInitialSize = 6;
        [SerializeField] private int _porkPoolMaxSize = 20;

        [Header("=== Visuals ===")]
        [SerializeField] private Vector3 _dustOffset = new Vector3(0f, 0.2f, 0f);
        [SerializeField] private Vector3 _dropOffset = new Vector3(0f, 0.1f, 0f);
        [SerializeField] private float _dropScatterRadius = 0.2f;

        [Header("=== Flash Effect ===")]
        [SerializeField] private Material _flashMaterial;
        [SerializeField] private float _flashDuration = 0.1f;
        [Tooltip("반짝임 강도 (0 = 없음, 1 = 최대)")]
        [Range(0f, 1f)]
        [SerializeField] private float _flashAmount = 1f;
        [Tooltip("반짝임 색상")]
        [SerializeField] private Color _flashColor = Color.white;

        [Header("=== Timing Controls ===")]
        [Tooltip("피격 반응 지연 시간 - 플레이어 애니메이션과 맞추기 위한 딜레이")]
        [SerializeField] private float _hitReactionDelay = 0f;
        [Tooltip("데미지 적용 지연 시간 - 실제 피격 처리 전 딜레이")]
        [SerializeField] private float _damageApplyDelay = 0f;
        [Tooltip("반짝임 효과 시작 지연 시간")]
        [SerializeField] private float _flashDelay = 0f;
        [Tooltip("죽음 처리 지연 시간 - 마지막 피격 후 죽음까지 딜레이")]
        [SerializeField] private float _deathDelay = 0f;
        [Tooltip("죽음 이펙트 지연 시간 - 먼지 이펙트 등 발생 지연")]
        [SerializeField] private float _deathEffectDelay = 0f;

        [Inject(Optional = true)] private IPoolManager _poolManager;
        [Inject(Optional = true)] private GameDataClass _gameData;

        private AnimalBaseAI _animalBaseAI;
        private Collider2D[] _colliders;
        private Renderer[] _renderers;
        private SpriteRenderer[] _spriteRenderers;
        private Material[][] _originalMaterials;
        private int _currentHits;
        private bool _isDead;
        private bool _isFlashing;

        public bool IsAlive => !_isDead;

        private void Awake()
        {
            _animalBaseAI = GetComponent<AnimalBaseAI>();
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void OnEnable()
        {
            ResolveDependencies();
            ResetState();
        }

        private void Start()
        {
            ResolveDependencies();
        }

        public bool TryHit()
        {
            if (_isDead || _animalBaseAI == null || _animalBaseAI.IsBaby)
            {
                return false;
            }

            StartCoroutine(ProcessHit());
            return true;
        }

        private System.Collections.IEnumerator ProcessHit()
        {
            // 1단계: 피격 반응 지연 (플레이어 애니메이션과 동기화)
            if (_hitReactionDelay > 0)
            {
                yield return new WaitForSeconds(_hitReactionDelay);
            }

            // 2단계: 데미지 적용 지연
            if (_damageApplyDelay > 0)
            {
                yield return new WaitForSeconds(_damageApplyDelay);
            }

            _currentHits++;

            // 3단계: 반짝임 효과 (지연 적용)
            if (_flashDelay > 0)
            {
                StartCoroutine(TriggerFlashWithDelay());
            }
            else
            {
                TriggerFlashEffect();
            }

            // 4단계: 죽음 체크 및 처리
            if (_currentHits >= Mathf.Max(1, _hitsToKill))
            {
                if (_deathDelay > 0)
                {
                    yield return new WaitForSeconds(_deathDelay);
                }
                Die();
            }
        }

        private System.Collections.IEnumerator TriggerFlashWithDelay()
        {
            yield return new WaitForSeconds(_flashDelay);
            TriggerFlashEffect();
        }

        public void ResetState()
        {
            _isDead = false;
            _currentHits = 0;

            if (_colliders != null)
            {
                foreach (Collider2D collider in _colliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = true;
                    }
                }
            }

            if (_renderers != null)
            {
                foreach (Renderer rendererComponent in _renderers)
                {
                    if (rendererComponent != null)
                    {
                        rendererComponent.enabled = true;
                    }
                }
            }
        }

        private void Die()
        {
            _isDead = true;
            SetVisible(false);
            
            if (_deathEffectDelay > 0)
            {
                StartCoroutine(SpawnDeathEffectWithDelay());
            }
            else
            {
                SpawnDeathEffect();
            }
            
            SpawnDrops();
            ReturnToPoolOrDisable();
        }

        private System.Collections.IEnumerator SpawnDeathEffectWithDelay()
        {
            yield return new WaitForSeconds(_deathEffectDelay);
            SpawnDeathEffect();
        }

        private void SetVisible(bool visible)
        {
            if (_colliders != null)
            {
                foreach (Collider2D collider in _colliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = visible;
                    }
                }
            }

            if (_renderers != null)
            {
                foreach (Renderer rendererComponent in _renderers)
                {
                    if (rendererComponent != null)
                    {
                        rendererComponent.enabled = visible;
                    }
                }
            }
        }

        private void SpawnDeathEffect()
        {
            if (ResolvePoolManager() && _poolManager.GetPool(_dustPoolName) != null)
            {
                _poolManager.Spawn(_dustPoolName, transform.position + _dustOffset, Quaternion.identity);
            }
        }

        private void SpawnDrops()
        {
            int dropCount = Random.Range(_minDropAmount, _maxDropAmount + 1);
            if (Random.value <= _bonusDropChance)
            {
                dropCount += _bonusDropAmount;
            }

            if (dropCount <= 0)
            {
                return;
            }

            GameObject porkPrefab = ResolvePorkPrefab();
            if (_poolManager != null && porkPrefab != null && _poolManager.GetPool(_porkPoolName) == null)
            {
                _poolManager.CreatePool(_porkPoolName, porkPrefab, _porkPoolInitialSize, _porkPoolMaxSize);
            }

            for (int i = 0; i < dropCount; i++)
            {
                Vector2 scatter = Random.insideUnitCircle * _dropScatterRadius;
                Vector3 spawnPosition = transform.position + _dropOffset + new Vector3(scatter.x, scatter.y, 0f);

                GameObject droppedObject = null;
                if (ResolvePoolManager() && _poolManager.GetPool(_porkPoolName) != null)
                {
                    droppedObject = _poolManager.Spawn(_porkPoolName, spawnPosition, Quaternion.identity);
                }
                else if (porkPrefab != null)
                {
                    droppedObject = Instantiate(porkPrefab, spawnPosition, Quaternion.identity);
                }

                if (droppedObject == null)
                {
                    continue;
                }

                PickableItem pickableItem = droppedObject.GetComponent<PickableItem>();
                if (pickableItem == null)
                {
                    pickableItem = droppedObject.AddComponent<PickableItem>();
                }

                pickableItem.ConfigureDrop(_dropItemId, 1, _porkPoolName);
            }
        }

        private GameObject ResolvePorkPrefab()
        {
            ResolveGameData();
            return _gameData?.GetItem(_dropItemId)?.WorldPrefab;
        }

        private void ReturnToPoolOrDisable()
        {
            if (ResolvePoolManager() && _poolManager.GetPool(_pigPoolName) != null)
            {
                _poolManager.Despawn(_pigPoolName, gameObject);
                return;
            }

            gameObject.SetActive(false);
        }

        private void ResolveDependencies()
        {
            DIContainer.Inject(this);
            ResolvePoolManager();
            ResolveGameData();
        }

        private bool ResolvePoolManager()
        {
            if (_poolManager != null)
            {
                return true;
            }

            if (DIContainer.TryResolve(out _poolManager) && _poolManager != null)
            {
                return true;
            }

            _poolManager = PoolManager.Instance;
            return _poolManager != null;
        }

        private bool ResolveGameData()
        {
            if (_gameData != null)
            {
                return true;
            }

            if (DIContainer.TryResolve(out _gameData) && _gameData != null)
            {
                return true;
            }

            _gameData = Resources.Load<GameDataClass>("GameData/GameData");
            return _gameData != null;
        }

        private void TriggerFlashEffect()
        {
            if (_isFlashing || _flashMaterial == null || _spriteRenderers == null || _spriteRenderers.Length == 0)
            {
                return;
            }

            _isFlashing = true;
            StoreOriginalMaterials();
            ApplyFlashMaterial();
            StartCoroutine(RestoreOriginalMaterialsAfterDelay());
        }

        private void StoreOriginalMaterials()
        {
            _originalMaterials = new Material[_spriteRenderers.Length][];
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                {
                    _originalMaterials[i] = _spriteRenderers[i].materials;
                }
            }
        }

        private void ApplyFlashMaterial()
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                {
                    Material[] flashMaterials = new Material[_spriteRenderers[i].materials.Length];
                    for (int j = 0; j < flashMaterials.Length; j++)
                    {
                        // 머터리얼 인스턴스 생성 (속성 변경을 위해)
                        Material materialInstance = new Material(_flashMaterial);
                        
                        // Flash Amount 설정
                        materialInstance.SetFloat("_Flash_Amount", _flashAmount);
                        
                        // 색상 설정 (White 프로퍼티 사용)
                        materialInstance.SetColor("White", _flashColor);
                        
                        flashMaterials[j] = materialInstance;
                    }
                    _spriteRenderers[i].materials = flashMaterials;
                }
            }
        }

        private System.Collections.IEnumerator RestoreOriginalMaterialsAfterDelay()
        {
            yield return new WaitForSeconds(_flashDuration);
            RestoreOriginalMaterials();
            _isFlashing = false;
        }

        private void RestoreOriginalMaterials()
        {
            if (_originalMaterials == null || _spriteRenderers == null)
            {
                return;
            }

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null && i < _originalMaterials.Length && _originalMaterials[i] != null)
                {
                    _spriteRenderers[i].materials = _originalMaterials[i];
                }
            }
        }
    }
}
