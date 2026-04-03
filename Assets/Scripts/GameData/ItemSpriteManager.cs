using System.Collections.Generic;
using UnityEngine;
using DI;

namespace SunnysideIsland.GameData
{
    [System.Serializable]
    public class ItemMapping
    {
        public string itemId;
        public string itemName;
        public Sprite icon;
    }

    public interface IItemSpriteManager
    {
        Sprite GetSprite(string itemId);
        string GetItemName(string itemId);
        bool HasSprite(string itemId);
        IReadOnlyDictionary<string, Sprite> AllSprites { get; }
    }

    public class ItemSpriteManager : MonoBehaviour, IItemSpriteManager
    {
        [Header("=== Item Mappings ===")]
        [SerializeField] private List<ItemMapping> _itemMappings = new List<ItemMapping>();

        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, string> _itemNames = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, Sprite> AllSprites => _sprites;

        private void Awake()
        {
            LoadFromInspector();
            LoadAllSprites();
        }

        private void LoadFromInspector()
        {
            _sprites.Clear();
            _itemNames.Clear();

            if (_itemMappings == null) return;

            foreach (var mapping in _itemMappings)
            {
                if (string.IsNullOrEmpty(mapping.itemId)) continue;

                if (mapping.icon != null && !_sprites.ContainsKey(mapping.itemId))
                {
                    _sprites[mapping.itemId] = mapping.icon;
                }

                if (!string.IsNullOrEmpty(mapping.itemName) && !_itemNames.ContainsKey(mapping.itemId))
                {
                    _itemNames[mapping.itemId] = mapping.itemName;
                }
            }
        }

        private void LoadAllSprites()
        {
            LoadSpritesFromResources("Icons/Items");
            LoadSpritesFromResources("Sprites/Items");
        }

        private void LoadSpritesFromResources(string path)
        {
            var sprites = Resources.LoadAll<Sprite>(path);
            foreach (var sprite in sprites)
            {
                string itemId = GetItemIdFromSpriteName(sprite.name);
                if (!string.IsNullOrEmpty(itemId) && !_sprites.ContainsKey(itemId))
                {
                    _sprites[itemId] = sprite;
                }
            }
        }

        private string GetItemIdFromSpriteName(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return null;

            string lower = spriteName.ToLower();
            if (lower.Contains("potato"))
                return "item_potato";
            if (lower.Contains("wood"))
                return "item_wood";
            if (lower.Contains("carrot"))
                return "item_carrot";
            if (lower.Contains("cabbage"))
                return "cabbage";
            if (lower.Contains("pumpkin"))
                return "pumpkin";
            if (lower.Contains("wheat"))
                return "wheat";
            if (lower.Contains("egg"))
                return "item_egg";

            return "item_" + spriteName;
        }

        public Sprite GetSprite(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            if (_sprites.TryGetValue(itemId, out var sprite))
                return sprite;

            if (!itemId.StartsWith("item_"))
            {
                string prefixedId = "item_" + itemId;
                if (_sprites.TryGetValue(prefixedId, out sprite))
                    return sprite;
            }

            return null;
        }

        public bool HasSprite(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _sprites.ContainsKey(itemId);
        }

        public string GetItemName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return string.Empty;
            
            if (_itemNames.TryGetValue(itemId, out var name))
                return name;

            if (!itemId.StartsWith("item_"))
            {
                string prefixedId = "item_" + itemId;
                if (_itemNames.TryGetValue(prefixedId, out name))
                    return name;
            }

            return itemId;
        }
    }
}
