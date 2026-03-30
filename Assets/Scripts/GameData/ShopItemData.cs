using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class ShopItemData
    {
        [Column("ShopItemID")]
        public string shopItemId;
        
        [Column("ItemID")]
        public string itemId;
        
        [Column("BuyPrice")]
        public int buyPrice;
        
        [Column("SellPrice")]
        public int sellPrice;
        
        [Column("StockAmount")]
        public int stockAmount;
        
        [Column("RestockInterval")]
        public int restockInterval;
    }
}
