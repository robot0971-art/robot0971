using UnityEngine;

namespace SunnysideIsland.Pool
{
    public class DroppedItem : PoolableObject
    {
        [Header("=== Item Settings ===")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _pickupDelay = 0.5f;
        [SerializeField] private float _despawnTime = 60f;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobHeight = 0.1f;
        
        private string _itemId;
        private int _quantity;
        private float _pickupTimer;
        private float _despawnTimer;
        private Vector3 _basePosition;
        private bool _canPickup;
        
        public string ItemId => _itemId;
        public int Quantity => _quantity;
        public bool CanPickup => _canPickup;
        
        public void Initialize(string itemId, int quantity, Sprite icon = null)
        {
            _itemId = itemId;
            _quantity = quantity;
            
            if (_spriteRenderer != null && icon != null)
            {
                _spriteRenderer.sprite = icon;
            }
        }
        
        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            
            _pickupTimer = _pickupDelay;
            _despawnTimer = _despawnTime;
            _canPickup = false;
            _basePosition = transform.position;
        }
        
        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
            
            _itemId = null;
            _quantity = 0;
            _canPickup = false;
        }
        
        private void Update()
        {
            if (!_canPickup)
            {
                _pickupTimer -= Time.deltaTime;
                if (_pickupTimer <= 0f)
                {
                    _canPickup = true;
                }
            }
            
            _despawnTimer -= Time.deltaTime;
            if (_despawnTimer <= 0f)
            {
                ReturnToPool();
                return;
            }
            
            float bobOffset = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
            transform.position = _basePosition + Vector3.up * bobOffset;
        }
        
        public void SetDespawnTime(float time)
        {
            _despawnTime = time;
        }
        
        public int PickUp(int amount)
        {
            int picked = Mathf.Min(amount, _quantity);
            _quantity -= picked;
            
            if (_quantity <= 0)
            {
                ReturnToPool();
            }
            
            return picked;
        }
    }
}