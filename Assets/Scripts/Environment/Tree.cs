using System;
using System.Collections;
using UnityEngine;
using DI;
using Newtonsoft.Json.Linq;
using SunnysideIsland.Building;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.Pool;

namespace SunnysideIsland.Environment
{
    public class Tree : MonoBehaviour, ISaveable
    {
        [Header("=== Settings ===")]
        [SerializeField] private int _maxHits = 5;
        [SerializeField] private GameObject _woodPrefab;
        [SerializeField] private int _woodAmount = 3;
        [SerializeField] private int _minRespawnDays = 1;
        [SerializeField] private int _maxRespawnDays = 3;
        [SerializeField] private LayerMask _respawnBlockingLayers;
        [SerializeField] private Vector2 _respawnBoundsPadding = new Vector2(0.4f, 0.4f);
        [SerializeField] private float _campfireCheckRadius = 0.75f;

        [Header("=== Visual Effects ===")]
        [SerializeField] private float _flashDuration = 0.15f;
        [SerializeField] private float _shakeIntensity = 0.05f;
        [SerializeField] private float _fallDelay = 0.4f;
        [SerializeField] private float _hitDelay = 0.15f;
        [SerializeField] private Vector3 _dustOffset = new Vector3(0f, 0.5f, 0f);

        private static readonly int FlashAmountProp = Shader.PropertyToID("_Flash_Amount");

        private int _currentHits;
        private bool _isChopped;
        private bool _isFalling;
        private int _respawnDay = -1;
        private SpriteRenderer _spriteRenderer;
        private Material _hitMaterial;
        private Coroutine _flashCoroutine;
        private Collider2D[] _colliders;

        [Inject(Optional = true)] private IPoolManager _poolManager;
        [Inject(Optional = true)] private TimeManager _timeManager;

        public bool IsChopped => _isChopped;

        [SerializeField] private string _uniqueId;
        public string SaveKey => string.IsNullOrEmpty(_uniqueId)
            ? $"Tree_{Mathf.RoundToInt(transform.position.x)}_{Mathf.RoundToInt(transform.position.y)}_{Mathf.RoundToInt(transform.position.z)}"
            : _uniqueId;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _hitMaterial = _spriteRenderer.material;
            }

            _colliders = GetComponentsInChildren<Collider2D>();
        }

        private void Start()
        {
            DIContainer.Inject(this);
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
        }

        public void Chop()
        {
            if (_isChopped || _isFalling)
            {
                return;
            }

            _currentHits++;
            PlayHitEffect();

            Debug.Log($"[Tree] Hit {_currentHits}/{_maxHits}");

            if (_currentHits >= _maxHits)
            {
                _isFalling = true;
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
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(HitEffectRoutine());
        }

        private IEnumerator HitEffectRoutine()
        {
            if (_hitMaterial == null)
            {
                yield break;
            }

            yield return new WaitForSeconds(_hitDelay);

            _hitMaterial.SetFloat(FlashAmountProp, 1.0f);

            Vector3 originalPos = transform.position;
            transform.position += new Vector3(UnityEngine.Random.Range(-_shakeIntensity, _shakeIntensity), 0f, 0f);

            yield return new WaitForSeconds(_flashDuration);

            _hitMaterial.SetFloat(FlashAmountProp, 0.0f);
            transform.position = originalPos;
        }

        private void FallTree()
        {
            Debug.Log($"[Tree] FallTree called, _poolManager={_poolManager != null}");

            _isChopped = true;
            _respawnDay = CalculateRespawnDay();

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

            SetTreeVisible(false);

            EventBus.Publish(new TreeChoppedEvent
            {
                TreePosition = transform.position,
                WoodAmount = _woodAmount
            });

            Debug.Log($"[Tree] Tree chopped! Spawned {_woodAmount} wood. Respawn day: {_respawnDay}");
        }

        private void Respawn()
        {
            SetTreeVisible(true);

            _isChopped = false;
            _isFalling = false;
            _currentHits = 0;
            _respawnDay = -1;

            Debug.Log("[Tree] Tree respawned");
        }

        private void SetTreeVisible(bool isVisible)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = isVisible;
            }

            if (_colliders == null)
            {
                return;
            }

            foreach (var col in _colliders)
            {
                col.enabled = isVisible;
            }
        }

        [Serializable]
        private class TreeSaveData
        {
            public int currentHits;
            public bool isChopped;
            public int respawnDay;
        }

        public object GetSaveData()
        {
            return new TreeSaveData
            {
                currentHits = _currentHits,
                isChopped = _isChopped,
                respawnDay = _respawnDay
            };
        }

        public void LoadSaveData(object state)
        {
            var data = state as TreeSaveData ?? (state as JObject)?.ToObject<TreeSaveData>();
            if (data == null)
            {
                return;
            }

            _currentHits = data.currentHits;
            _isChopped = data.isChopped;
            _respawnDay = data.respawnDay;
            _isFalling = false;

            if (_isChopped)
            {
                SetTreeVisible(false);

                if (ShouldRespawnToday())
                {
                    Respawn();
                }

                return;
            }

            SetTreeVisible(true);
        }

        private void OnDayStarted(DayStartedEvent evt)
        {
            if (!_isChopped)
            {
                return;
            }

            if (_respawnDay > 0 && evt.Day >= _respawnDay && CanRespawnHere())
            {
                Respawn();
            }
        }

        private int CalculateRespawnDay()
        {
            int currentDay = _timeManager != null ? _timeManager.CurrentDay : 0;
            int minDays = Mathf.Max(1, _minRespawnDays);
            int maxDays = Mathf.Max(minDays, _maxRespawnDays);
            int randomDays = UnityEngine.Random.Range(minDays, maxDays + 1);
            return currentDay + randomDays;
        }

        private bool ShouldRespawnToday()
        {
            if (_respawnDay <= 0)
            {
                return true;
            }

            if (_timeManager == null)
            {
                return false;
            }

            return _timeManager.CurrentDay >= _respawnDay && CanRespawnHere();
        }

        private bool CanRespawnHere()
        {
            float candidateRadius = Mathf.Max(
                0.1f,
                Mathf.Max(_respawnBoundsPadding.x, _respawnBoundsPadding.y) + 0.5f);

            var blockingHits = Physics2D.OverlapCircleAll(transform.position, candidateRadius, _respawnBlockingLayers);

            foreach (var hit in blockingHits)
            {
                if (hit == null)
                {
                    continue;
                }

                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (IsInsideExpandedBounds(hit))
                {
                    return false;
                }
            }

            float campfireCheckRadius = Mathf.Max(0.05f, _campfireCheckRadius);
            var nearbyHits = Physics2D.OverlapCircleAll(transform.position, campfireCheckRadius);
            foreach (var hit in nearbyHits)
            {
                if (hit == null)
                {
                    continue;
                }

                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hit.GetComponentInParent<Campfire>() != null)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsInsideExpandedBounds(Collider2D hit)
        {
            Bounds expandedBounds = hit.bounds;
            Vector3 padding = new Vector3(
                Mathf.Max(0f, _respawnBoundsPadding.x),
                Mathf.Max(0f, _respawnBoundsPadding.y),
                0f);

            expandedBounds.Expand(padding);
            return expandedBounds.Contains(transform.position);
        }
    }

    public class TreeChoppedEvent
    {
        public Vector3 TreePosition { get; set; }
        public int WoodAmount { get; set; }
    }
}
