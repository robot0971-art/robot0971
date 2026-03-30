using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class ItemData
    {
        [Column("ItemID")]
        public string itemId;
        
        [Column("ItemName")]
        public string itemName;
        
        [Column("ItemType")]
        public ItemType itemType;
        
        [Column("MaxStack")]
        public int maxStack;
        
        [Column("BaseValue")]
        public int baseValue;
        
        [Column("CanSell")]
        public bool canSell;
        
        [Column("Description")]
        public string description;
        
        [Column("IconPath")]
        public string iconPath;
        
        // Addressable 참조
        [SerializeField]
        private AssetReference _iconReference;
        public AssetReference IconReference { get => _iconReference; set => _iconReference = value; }
        
        // 런타임 캐싱
        [NonSerialized] private Sprite _cachedIcon;
        [NonSerialized] private bool _isIconLoading;
        
        public async Task<Sprite> LoadIconAsync()
        {
            if (_cachedIcon != null)
                return _cachedIcon;
            
            if (_isIconLoading)
            {
                while (_isIconLoading)
                {
                    await Task.Yield();
                }
                return _cachedIcon;
            }
            
            _isIconLoading = true;
            
            try
            {
                // [주석] Addressables 로직 - ItemSpriteManager 사용으로 대체
                /*
                if (_iconReference != null && _iconReference.RuntimeKeyIsValid())
                {
                    try
                    {
                        var handle = _iconReference.LoadAssetAsync<Sprite>();
                        _cachedIcon = await handle.Task;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ItemData] Failed to load icon: {ex.Message}");
                        _cachedIcon = null;
                    }
                }
                else if (!string.IsNullOrEmpty(iconPath))
                */
                if (!string.IsNullOrEmpty(iconPath))
                {
                    _cachedIcon = Resources.Load<Sprite>(iconPath);
                }
            }
            finally
            {
                _isIconLoading = false;
            }
            
            return _cachedIcon;
        }
        
        public Sprite GetIcon()
        {
            return _cachedIcon;
        }
        
        public void ReleaseIcon()
        {
            if (_cachedIcon != null && _iconReference != null)
            {
                _iconReference.ReleaseAsset();
                _cachedIcon = null;
            }
        }
        
        public bool HasValidIconReference()
        {
            return _iconReference != null && _iconReference.RuntimeKeyIsValid();
        }
    }

    public enum ItemType
    {
        Tool,
        Consumable,
        Material,
        Equipment,
        Valuable,
        Quest
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
