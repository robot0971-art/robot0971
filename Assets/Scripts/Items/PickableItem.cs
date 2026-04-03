using DI;
using SunnysideIsland.Inventory;
using SunnysideIsland.Pool;
using UnityEngine;

namespace SunnysideIsland.Items
{
    public class PickableItem : MonoBehaviour
    {
        [Header("=== Settings ===")]
        [SerializeField] private string _itemId = "wood";
        [SerializeField] private int _quantity = 1;
        [SerializeField] private string _poolName = "Wood";
        [SerializeField] private string _pickupLayerName = "Harvest";

        public string ItemId => _itemId;
        public int Quantity => _quantity;

        [Inject] private IInventorySystem _inventory;
        [Inject] private IPoolManager _poolManager;

        private void OnEnable()
        {
            EnsurePickupLayer();
        }

        private void Start()
        {
            DIContainer.Inject(this);
            EnsurePickupLayer();
        }

        public void ConfigureDrop(string itemId, int quantity, string poolName = null)
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                _itemId = itemId;
            }

            _quantity = Mathf.Max(1, quantity);

            if (!string.IsNullOrEmpty(poolName))
            {
                _poolName = poolName;
            }

            DIContainer.Inject(this);
            EnsurePickupLayer();
        }

        public void PickUp()
        {
            if (_inventory == null)
            {
                Debug.LogWarning("[PickableItem] No InventorySystem injected");
                return;
            }

            bool added = _inventory.AddItem(_itemId, _quantity);
            if (!added)
            {
                Debug.LogWarning($"[PickableItem] Failed to add {_itemId} to inventory");
                return;
            }

            if (_poolManager != null && _poolManager.GetPool(_poolName) != null)
            {
                _poolManager.Despawn(_poolName, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void EnsurePickupLayer()
        {
            int pickupLayer = LayerMask.NameToLayer(_pickupLayerName);
            if (pickupLayer >= 0)
            {
                gameObject.layer = pickupLayer;
            }
        }
    }
}
