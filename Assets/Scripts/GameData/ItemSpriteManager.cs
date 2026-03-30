using System.Collections.Generic;
using UnityEngine;
using DI;

namespace SunnysideIsland.GameData
{
    public interface IItemSpriteManager
    {
        Sprite GetSprite(string itemId);
        string GetItemName(string itemId);
        bool HasSprite(string itemId);
        IReadOnlyDictionary<string, Sprite> AllSprites { get; }
    }

    public class ItemSpriteManager : MonoBehaviour, IItemSpriteManager
    {
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        
        private readonly Dictionary<string, string> _itemNames = new Dictionary<string, string>
        {
            { "item_potato", "감자" },
            { "item_wood", "나무" },
            { "item_carrot", "당근" },
            { "item_egg", "계란" }
        };

        public IReadOnlyDictionary<string, Sprite> AllSprites => _sprites;

        private void Awake()
        {
            Debug.Log("[ItemSpriteManager] Awake() 호출됨");
            LoadAllSprites();
            Debug.Log($"[ItemSpriteManager] 로드된 스프라이트 수: {_sprites.Count}");
            foreach (var kvp in _sprites)
            {
                Debug.Log($"[ItemSpriteManager] 등록됨: {kvp.Key}");
            }
        }

        private void LoadAllSprites()
        {
            LoadSpritesFromResources("Icons/Items");
            LoadSpritesFromResources("Sprites/Items");
            LoadSpritesFromAddItemFolder();
        }

        private void LoadSpritesFromResources(string path)
        {
            var sprites = Resources.LoadAll<Sprite>(path);
            Debug.Log($"[ItemSpriteManager] LoadSpritesFromResources({path}) - Found {sprites.Length} sprites");
            foreach (var sprite in sprites)
            {
                string itemId = GetItemIdFromSpriteName(sprite.name);
                Debug.Log($"[ItemSpriteManager] Sprite: {sprite.name} -> itemId: {itemId}");
                if (!string.IsNullOrEmpty(itemId) && !_sprites.ContainsKey(itemId))
                {
                    _sprites[itemId] = sprite;
                }
            }
        }

        private void LoadSpritesFromAddItemFolder()
        {
            var guids = new Dictionary<string, string>
            {
                { "item_potato", "5790871d326542e47b60ed0ed6ab8ff5" },
                { "item_wood", "1f619268bdc7f3741a6e55a3d22f95aa" }
            };

            foreach (var kvp in guids)
            {
                if (_sprites.ContainsKey(kvp.Key))
                    continue;

                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(kvp.Value);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    if (texture != null)
                    {
                        Sprite sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );
                        _sprites[kvp.Key] = sprite;
                    }
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
            if (lower.Contains("egg"))
                return "item_egg";

            return "item_" + spriteName;
        }

        public Sprite GetSprite(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            // 직접 검색
            if (_sprites.TryGetValue(itemId, out var sprite))
                return sprite;

            // "item_" 접두사 없으면 추가해서 검색
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
            
            // 직접 검색
            if (_itemNames.TryGetValue(itemId, out var name))
                return name;

            // "item_" 접두사 없으면 추가해서 검색
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