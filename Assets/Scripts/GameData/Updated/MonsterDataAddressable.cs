using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SunnysideIsland.GameData
{
    /// <summary>
    /// Addressable을 지원하는 몬스터 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class MonsterDataAddressable
    {
        public string monsterId;
        public string monsterName;
        public int hp;
        public int attackPower;
        public int defense;
        public float speed;
        public int expReward;
        public string aiType;
        
        // 드랍 아이템
        public List<DropItem> dropItems;
        
        // 기존 string path (하위 호환성)
        public string prefabPath;
        public string iconPath;
        
        // Addressable 참조 (새로운 방식)
        public AssetReferenceGameObject prefabReference;
        public AssetReferenceT<Sprite> iconReference;
        public List<AssetReferenceGameObject> dropItemPrefabReferences;
        
        // 런타임 캐싱
        [System.NonSerialized] private GameObject _cachedPrefab;
        [System.NonSerialized] private Sprite _cachedIcon;
        
        /// <summary>
        /// 몬스터 프리팹을 비동기로 로드합니다.
        /// </summary>
        public async Task<GameObject> LoadPrefabAsync()
        {
            if (_cachedPrefab != null)
                return _cachedPrefab;
            
            if (prefabReference != null && prefabReference.RuntimeKeyIsValid())
            {
                var handle = prefabReference.LoadAssetAsync<GameObject>();
                _cachedPrefab = await handle.Task;
            }
            else if (!string.IsNullOrEmpty(prefabPath))
            {
                _cachedPrefab = Resources.Load<GameObject>(prefabPath);
            }
            
            return _cachedPrefab;
        }
        
        /// <summary>
        /// 몬스터를 인스턴스화합니다.
        /// </summary>
        public async Task<GameObject> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefabReference != null && prefabReference.RuntimeKeyIsValid())
            {
                var handle = prefabReference.InstantiateAsync(position, rotation, parent);
                return await handle.Task;
            }
            else if (!string.IsNullOrEmpty(prefabPath))
            {
                var prefab = await LoadPrefabAsync();
                if (prefab != null)
                {
                    return Object.Instantiate(prefab, position, rotation, parent);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 아이콘을 비동기로 로드합니다.
        /// </summary>
        public async Task<Sprite> LoadIconAsync()
        {
            if (_cachedIcon != null)
                return _cachedIcon;
            
            if (iconReference != null && iconReference.RuntimeKeyIsValid())
            {
                var handle = iconReference.LoadAssetAsync<Sprite>();
                _cachedIcon = await handle.Task;
            }
            else if (!string.IsNullOrEmpty(iconPath))
            {
                _cachedIcon = Resources.Load<Sprite>(iconPath);
            }
            
            return _cachedIcon;
        }
        
        /// <summary>
        /// 캐시된 에셋을 해제합니다.
        /// </summary>
        public void ReleaseAssets()
        {
            if (_cachedPrefab != null && prefabReference != null)
            {
                prefabReference.ReleaseAsset();
                _cachedPrefab = null;
            }
            
            if (_cachedIcon != null && iconReference != null)
            {
                iconReference.ReleaseAsset();
                _cachedIcon = null;
            }
        }
    }
    
    [System.Serializable]
    public class DropItem
    {
        public string itemId;
        public int minQuantity;
        public int maxQuantity;
        public float dropRate;
    }
}
