using System;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Inventory
{
    /// <summary>
    /// 인벤토리 슬롯 인터페이스
    /// </summary>
    public interface IInventorySlot
    {
        string ItemId { get; }
        int Quantity { get; }
        int MaxStack { get; }
        bool IsEmpty { get; }
        bool IsFull { get; }
        
        bool CanAdd(string itemId, int quantity);
        bool Add(string itemId, int quantity, int maxStack);
        bool Remove(int quantity);
        void Clear();
    }

    /// <summary>
    /// 인벤토리 슬롯
    /// </summary>
    [Serializable]
    public class InventorySlot : IInventorySlot
    {
        [SerializeField] private string _itemId;
        [SerializeField] private int _quantity;
        [SerializeField] private int _maxStack;
        
        public string ItemId => _itemId;
        public int Quantity => _quantity;
        public int MaxStack => _maxStack;
        public bool IsEmpty => string.IsNullOrEmpty(_itemId) || _quantity <= 0;
        public bool IsFull => _quantity >= _maxStack;
        
        public bool CanAdd(string itemId, int quantity)
        {
            if (IsEmpty) return true;
            return _itemId == itemId && _quantity + quantity <= _maxStack;
        }
        
        public bool Add(string itemId, int quantity, int maxStack)
        {
            if (!CanAdd(itemId, quantity)) return false;
            
            if (IsEmpty)
            {
                _itemId = itemId;
                _maxStack = maxStack;
            }
            
            _quantity += quantity;
            return true;
        }
        
        public bool Remove(int quantity)
        {
            if (_quantity < quantity) return false;
            
            _quantity -= quantity;
            if (_quantity <= 0)
            {
                Clear();
            }
            return true;
        }
        
        public void Clear()
        {
            _itemId = null;
            _quantity = 0;
            _maxStack = 0;
        }
    }
}
