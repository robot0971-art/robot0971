using UnityEngine;
using SunnysideIsland.Events;
using SunnysideIsland.Animal;
using SunnysideIsland.Pool;
using SunnysideIsland.Inventory;
using SunnysideIsland.Core;

namespace SunnysideIsland.Items
{
    public class EggItem : PoolableObject
    {
        [Header("=== Egg Settings ===")]
        [SerializeField] private string _itemId = "egg";
        [SerializeField] private int _amount = 1;
        [SerializeField] private float _pickupRange = 1.5f;
        [SerializeField] private LayerMask _playerLayer;
        
        [Header("=== Hatching Settings ===")]
        [SerializeField] private int _hatchAfterDays = 1;
        [SerializeField] private GameObject _whiteChickenPrefab;
        [SerializeField] private GameObject _blackChickenPrefab;
        
        private Transform _playerTransform;
        private bool _canPickup = true;
        private EggPoint _parentEggPoint;
        private int _spawnDay = -1;
        private bool _isHatching = false;
        
        public string ItemId => _itemId;
        public int Amount => _amount;
        
        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            _canPickup = true;
            _playerTransform = null;
            _parentEggPoint = null;
            _spawnDay = -1;
            _isHatching = false;
        }
        
        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
            // EggPoint에게 알리기
            if (_parentEggPoint != null)
            {
                _parentEggPoint.OnEggCollected();
            }
            UnsubscribeFromEvents();
        }
        
        public void SetParentEggPoint(EggPoint eggPoint)
        {
            _parentEggPoint = eggPoint;
        }
        
        private void Start()
        {
            if (_playerLayer == 0)
                _playerLayer = LayerMask.GetMask("Player");
            
            // 부화 시작일 설정
            if (_spawnDay < 0)
            {
                var timeManager = UnityEngine.Object.FindObjectOfType<TimeManager>();
                if (timeManager != null)
                {
                    _spawnDay = timeManager.CurrentDay;
                }
            }
            
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void SubscribeToEvents()
        {
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
        }
        
        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
        }
        
        private void OnDayStarted(DayStartedEvent evt)
        {
            if (_isHatching) return;
            
            // 인벤토리에 있으면 부화하지 않음
            if (transform.parent != null && transform.parent.name.Contains("Inventory")) return;
            
            // 하루가 지났는지 확인
            if (_spawnDay >= 0 && evt.Day >= _spawnDay + _hatchAfterDays)
            {
                Hatch();
            }
        }
        
        private void Hatch()
        {
            _isHatching = true;
            _canPickup = false;
            
            Debug.Log($"[EggItem] Hatching at {transform.position}!");
            
            // 흰닭/까만닭 랜덤 선택
            bool isWhite = Random.value > 0.5f;
            GameObject chickenPrefab = isWhite ? _whiteChickenPrefab : _blackChickenPrefab;
            
            // 프리팹이 할당되지 않았으면 Resources에서 로드
            if (chickenPrefab == null)
            {
                string prefabName = isWhite ? "Prefabs/Animals/Chicken_White" : "Prefabs/Animals/Chicken_Black";
                chickenPrefab = Resources.Load<GameObject>(prefabName);
            }
            
            if (chickenPrefab != null)
            {
                var chicken = Instantiate(chickenPrefab, transform.position, Quaternion.identity);
                Debug.Log($"[EggItem] Spawned {(isWhite ? "White" : "Black")} chicken at {transform.position}!");
            }
            else
            {
                Debug.LogWarning("[EggItem] Chicken prefab not found!");
            }
            
            // Egg 제거 (Pool로 반환)
            if (_parentEggPoint != null)
            {
                _parentEggPoint.OnEggCollected();
            }
            ReturnToPool();
        }
        
        private void Update()
        {
            if (!_canPickup) return;
            
            // 플레이어 찾기
            if (_playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    _playerTransform = player.transform;
            }
            
            // 플레이어와의 거리 체크
            if (_playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, _playerTransform.position);
                if (distance <= _pickupRange)
                {
                    // 플레이어가 E 키를 누르면 줍기
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        TryPickup();
                    }
                }
            }
        }
        
        private void TryPickup()
        {
            if (!_canPickup) return;
            
            // 인벤토리 시스템 찾기
            var inventory = FindObjectOfType<InventorySystem>();
            if (inventory != null)
            {
                bool added = inventory.AddItem(_itemId, _amount);
                if (added)
                {
                    // 수확 애니메이션 재생
                    if (_playerTransform != null)
                    {
                        var animator = _playerTransform.GetComponent<Animator>();
                        if (animator != null)
                        {
                            animator.SetTrigger("Harvest");
                        }
                    }

                    // 이벤트 발행
                    EventBus.Publish(new ItemCollectedEvent 
                    { 
                        ItemId = _itemId, 
                        Amount = _amount,
                        Position = transform.position 
                    });
                    
                    // 풀에 반환
                    ReturnToPool();
                }
            }
            else
            {
                Debug.LogWarning("[EggItem] InventorySystem not found!");
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & _playerLayer) != 0)
            {
                // 플레이어 근처에 왔을 때 UI 표시 가능
                // 예: "E를 눌러 줍기"
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }
    
    public class ItemCollectedEvent
    {
        public string ItemId;
        public int Amount;
        public Vector3 Position;
    }
}
