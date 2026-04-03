using System.Collections;
using System.Collections.Generic;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Inventory;
using SunnysideIsland.Player;
using SunnysideIsland.Pool;
using UnityEngine;

namespace SunnysideIsland.Animal
{
    [System.Serializable]
    public class CowMilkRewardEntry
    {
        public string itemId = "milk";
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;
        [Min(0.01f)] public float weight = 1f;
        public bool showQuestionMarkEffect = false;

        public int GetAmount()
        {
            int min = Mathf.Max(1, minAmount);
            int max = Mathf.Max(min, maxAmount);
            return Random.Range(min, max + 1);
        }
    }

    [DisallowMultipleComponent]
    public class CowMilkInteractable : MonoBehaviour, IInteractable
    {
        [Header("=== Reward Settings ===")]
        [SerializeField] private float _milkCooldownSeconds = 90f;
        [SerializeField] private bool _startReady = true;
        [SerializeField] private List<CowMilkRewardEntry> _rewardEntries = new List<CowMilkRewardEntry>
        {
            new CowMilkRewardEntry
            {
                itemId = "milk",
                minAmount = 1,
                maxAmount = 2,
                weight = 1f,
                showQuestionMarkEffect = true
            }
        };

        [Header("=== Interaction Settings ===")]
        [SerializeField] private Vector2 _interactionBoxSize = new Vector2(1.2f, 1.2f);
        [SerializeField] private float _maxInteractionDistance = 1.25f;

        [Header("=== Reward Effect ===")]
        [SerializeField] private string _questionMarkPoolName = "Questionmark";
        [SerializeField] private GameObject _questionMarkPrefab;
        [SerializeField] private float _pauseDuration = 1.0f;
        [SerializeField] private Vector3 _effectOffset = new Vector3(0f, 1.8f, 0f);
        [SerializeField] private float _effectDisplayTime = 1.2f;

        [Inject(Optional = true)]
        private IInventorySystem _inventorySystem;
        [Inject(Optional = true)]
        private IPoolManager _poolManager;
        private AnimalBaseAI _animalBaseAI;
        private PlayerController _playerController;
        private float _nextMilkTime;

        private void Awake()
        {
            DIContainer.Inject(this);
            _nextMilkTime = _startReady ? 0f : Time.time + _milkCooldownSeconds;
            EnsureInteractionSetup();
        }

        private void Start()
        {
            ResolveInventorySystem();
            ResolvePoolManager();
            ResolvePlayerController();
            ResolveAnimalBaseAI();
        }

        public void Interact()
        {
            if (!CanInteract())
            {
                return;
            }

            if (!ResolveInventorySystem())
            {
                Debug.LogWarning("[CowMilkInteractable] InventorySystem not found.");
                return;
            }

            CowMilkRewardEntry reward = SelectReward();
            if (reward == null)
            {
                Debug.LogWarning("[CowMilkInteractable] No reward entry configured.");
                return;
            }

            int rewardAmount = reward.GetAmount();
            if (!_inventorySystem.AddItem(reward.itemId, rewardAmount))
            {
                Debug.Log($"[CowMilkInteractable] Could not add {reward.itemId} to inventory.");
                return;
            }

            _nextMilkTime = Time.time + _milkCooldownSeconds;
            if (reward.showQuestionMarkEffect)
            {
                ShowQuestionMarkEffect();
            }

            Debug.Log($"[CowMilkInteractable] Collected {rewardAmount} {reward.itemId} from {gameObject.name}");
        }

        public bool CanInteract()
        {
            if (ResolveAnimalBaseAI() && !_animalBaseAI.CanProvideHarvestProducts)
            {
                return false;
            }

            if (!IsPlayerInRange())
            {
                return false;
            }

            return Time.time >= _nextMilkTime;
        }

        public string GetInteractionHint()
        {
            if (CanInteract())
            {
                return "Collect Milk";
            }

            if (ResolveAnimalBaseAI() && _animalBaseAI.IsBaby)
            {
                return "Too Young";
            }

            if (!IsPlayerInRange())
            {
                return "Too Far";
            }

            float remainingSeconds = Mathf.Max(0f, _nextMilkTime - Time.time);
            int remainingWholeSeconds = Mathf.CeilToInt(remainingSeconds);
            return $"Milk Ready In ({remainingWholeSeconds}s)";
        }

        private bool ResolveInventorySystem()
        {
            if (_inventorySystem != null)
            {
                return true;
            }

            return DIContainer.TryResolve(out _inventorySystem);
        }

        private bool ResolvePoolManager()
        {
            if (_poolManager != null)
            {
                return true;
            }

            return DIContainer.TryResolve(out _poolManager);
        }

        private bool ResolvePlayerController()
        {
            if (_playerController != null)
            {
                return true;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return false;
            }

            _playerController = player.GetComponent<PlayerController>();
            return _playerController != null;
        }

        private bool ResolveAnimalBaseAI()
        {
            if (_animalBaseAI != null)
            {
                return true;
            }

            _animalBaseAI = GetComponentInParent<AnimalBaseAI>();
            return _animalBaseAI != null;
        }

        private CowMilkRewardEntry SelectReward()
        {
            if (_rewardEntries == null || _rewardEntries.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            foreach (var entry in _rewardEntries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.itemId) || entry.weight <= 0f)
                {
                    continue;
                }

                totalWeight += entry.weight;
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var entry in _rewardEntries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.itemId) || entry.weight <= 0f)
                {
                    continue;
                }

                cumulative += entry.weight;
                if (roll <= cumulative)
                {
                    return entry;
                }
            }

            return _rewardEntries[0];
        }

        private bool IsPlayerInRange()
        {
            if (!ResolvePlayerController())
            {
                return true;
            }

            Vector2 playerPosition = _playerController.transform.position;
            Collider2D targetCollider = GetComponent<Collider2D>();
            Vector2 targetPoint = targetCollider != null
                ? targetCollider.ClosestPoint(playerPosition)
                : (Vector2)transform.position;

            return Vector2.Distance(playerPosition, targetPoint) <= _maxInteractionDistance;
        }

        private void EnsureInteractionSetup()
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                gameObject.layer = interactableLayer;
            }

            Collider2D existingCollider = GetComponent<Collider2D>();
            bool hasUsableCollider = existingCollider != null &&
                                     !string.Equals(existingCollider.GetType().Name, "TilemapCollider2D");

            if (hasUsableCollider)
            {
                existingCollider.isTrigger = true;
                return;
            }

            if (existingCollider != null)
            {
                existingCollider.enabled = false;
            }

            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            boxCollider.isTrigger = true;
            boxCollider.size = _interactionBoxSize;
        }

        private void ShowQuestionMarkEffect()
        {
            if (ResolvePlayerController())
            {
                _playerController.PauseMovement(_pauseDuration);
            }

            if (ResolvePoolManager())
            {
                if (_poolManager.GetPool(_questionMarkPoolName) == null && _questionMarkPrefab != null)
                {
                    _poolManager.CreatePool(_questionMarkPoolName, _questionMarkPrefab, 1, 10);
                }

                if (_poolManager.GetPool(_questionMarkPoolName) != null)
                {
                    Vector3 effectPosition = ResolvePlayerController()
                        ? _playerController.transform.position + _effectOffset
                        : transform.position + _effectOffset;

                    GameObject effect = _poolManager.Spawn(
                        _questionMarkPoolName,
                        effectPosition,
                        Quaternion.identity);

                    if (effect != null)
                    {
                        StartCoroutine(DespawnEffectRoutine(effect));
                        return;
                    }
                }
            }

            if (_questionMarkPrefab != null)
            {
                Vector3 effectPosition = ResolvePlayerController()
                    ? _playerController.transform.position + _effectOffset
                    : transform.position + _effectOffset;

                GameObject effect = Instantiate(
                    _questionMarkPrefab,
                    effectPosition,
                    Quaternion.identity);

                Destroy(effect, _effectDisplayTime);
            }
        }

        private IEnumerator DespawnEffectRoutine(GameObject effect)
        {
            yield return new WaitForSeconds(_effectDisplayTime);

            if (effect != null && effect.activeInHierarchy && _poolManager != null)
            {
                _poolManager.Despawn(_questionMarkPoolName, effect);
            }
        }
    }
}
