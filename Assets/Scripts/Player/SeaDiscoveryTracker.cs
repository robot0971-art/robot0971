using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.Inventory;
using SunnysideIsland.UI.Components;
using SunnysideIsland.Pool;
using GameDataAsset = SunnysideIsland.GameData.GameData;
using SeaDiscoveryEntry = SunnysideIsland.GameData.SeaDiscoveryData;

namespace SunnysideIsland.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class SeaDiscoveryTracker : MonoBehaviour
    {
        [Header("=== Discovery Timing ===")]
        [SerializeField] private float _checkInterval = 10f;
        [SerializeField] private float _baseChance = 0.15f;
        [SerializeField] private float _movementBonusPerMeter = 0.03f;
        [SerializeField] private float _maxChance = 0.6f;
        [SerializeField] private float _movementThreshold = 0.1f;

        [Header("=== Discovery Effects ===")]
        [SerializeField] private string _effectPoolName = "Questionmark";
        [SerializeField] private float _pauseDuration = 1.0f;
        [SerializeField] private Vector3 _effectOffset = new Vector3(0, 1.2f, 0);
        [SerializeField] private float _effectDisplayTime = 1.2f;

        [Inject] private GameDataAsset _gameData;
        [Inject(Optional = true)] private IInventorySystem _inventorySystem;
        [Inject(Optional = true)] private TimeManager _timeManager;
        [Inject(Optional = true)] private IPoolManager _poolManager;

        private PlayerController _player;
        private float _timer;
        private Vector3 _lastPosition;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        private void Start()
        {
            DIContainer.Inject(this);

            if (_inventorySystem == null) DIContainer.TryResolve(out _inventorySystem);
            if (_poolManager == null) DIContainer.TryResolve(out _poolManager);

            _timer = _checkInterval;
            _lastPosition = transform.position;
        }

        private void Update()
        {
            if (_player == null || _gameData == null || _inventorySystem == null) return;

            // 수영 중일 때만 작동
            if (!_player.IsSwimming)
            {
                _timer = _checkInterval;
                _lastPosition = transform.position;
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            TryDiscover();
            _timer = _checkInterval;
            _lastPosition = transform.position;
        }

        private void TryDiscover()
        {
            if (_gameData.seaDiscoveryItems == null || _gameData.seaDiscoveryItems.Count == 0) return;

            if (Random.value > CalculateChance()) return;

            Season currentSeason = _timeManager?.CurrentSeason ?? Season.Spring;
            var candidates = new List<SeaDiscoveryEntry>();

            foreach (var entry in _gameData.seaDiscoveryItems)
            {
                if (entry == null || entry.weight <= 0f) continue;
                if (!entry.IsAvailable(currentSeason)) continue;
                candidates.Add(entry);
            }

            if (candidates.Count == 0) return;

            var selection = SelectEntry(candidates);
            if (selection == null) return;

            // [발견 시 연출 실행]
            PerformDiscoveryEffect();

            // [아이템 즉시 획득]
            int quantity = selection.GetRandomQuantity();
            bool added = _inventorySystem.AddItem(selection.itemId, quantity);

            string itemName = _gameData.GetItem(selection.itemId)?.itemName ?? selection.itemId;
            var message = added
                ? $"바다에서 {itemName}을(를) 발견했습니다!"
                : $"인벤토리가 가득 찼습니다. {itemName}을(를) 버렸습니다.";

            ToastMessage.Instance?.ShowMessage(message);

            EventBus.Publish(new SeaDiscoveryEvent
            {
                ItemId = selection.itemId,
                ItemName = itemName,
                Quantity = quantity,
                WasAdded = added
            });
        }

        private void PerformDiscoveryEffect()
        {
            // 1. 플레이어 멈춤
            if (_player != null)
            {
                _player.PauseMovement(_pauseDuration);
            }

            // 2. 마크(Dust) 스폰
            if (_poolManager != null)
            {
                GameObject effect = _poolManager.Spawn(_effectPoolName, transform.position + _effectOffset, Quaternion.identity);
                if (effect != null)
                {
                    StartCoroutine(DespawnEffectRoutine(effect, _effectDisplayTime));
                }
            }
        }

        private IEnumerator DespawnEffectRoutine(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null && obj.activeInHierarchy)
            {
                _poolManager.Despawn(_effectPoolName, obj);
            }
        }

        private float CalculateChance()
        {
            float distance = Vector3.Distance(transform.position, _lastPosition);
            float bonusDistance = Mathf.Max(0f, distance - _movementThreshold);
            float chance = _baseChance + bonusDistance * _movementBonusPerMeter;
            return Mathf.Clamp(chance, 0f, _maxChance);
        }

        private static SeaDiscoveryEntry SelectEntry(List<SeaDiscoveryEntry> entries)
        {
            float totalWeight = 0f;
            foreach (var entry in entries) totalWeight += entry.weight;
            if (totalWeight <= 0f) return null;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var entry in entries)
            {
                cumulative += entry.weight;
                if (roll <= cumulative) return entry;
            }
            return entries[0];
        }
    }
}
