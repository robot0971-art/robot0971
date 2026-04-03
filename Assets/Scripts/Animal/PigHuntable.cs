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

        [Inject(Optional = true)] private IPoolManager _poolManager;
        [Inject(Optional = true)] private GameDataClass _gameData;

        private AnimalBaseAI _animalBaseAI;
        private Collider2D[] _colliders;
        private Renderer[] _renderers;
        private int _currentHits;
        private bool _isDead;

        public bool IsAlive => !_isDead;

        private void Awake()
        {
            _animalBaseAI = GetComponent<AnimalBaseAI>();
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
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

            _currentHits++;

            if (_currentHits >= Mathf.Max(1, _hitsToKill))
            {
                Die();
            }

            return true;
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
            SpawnDeathEffect();
            SpawnDrops();
            ReturnToPoolOrDisable();
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
    }
}
