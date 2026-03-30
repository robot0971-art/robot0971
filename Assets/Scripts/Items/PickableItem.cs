using UnityEngine;
using SunnysideIsland.Events;
using SunnysideIsland.Inventory;

namespace SunnysideIsland.Items
{
    public class PickableItem : MonoBehaviour
    {
        [Header("=== Settings ===")]
        [SerializeField] private string _itemId = "wood";
        [SerializeField] private int _quantity = 1;
        
        public string ItemId => _itemId;
        public int Quantity => _quantity;
        
        public void PickUp()
        {
            var inventory = Object.FindObjectOfType<InventorySystem>();
            if (inventory != null)
            {
                bool added = inventory.AddItem(_itemId, _quantity);
                if (added)
                {
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning($"[PickableItem] Failed to add {_itemId} to inventory");
                }
            }
            else
            {
                Debug.LogWarning("[PickableItem] No InventorySystem found");
            }
        }
    }
}