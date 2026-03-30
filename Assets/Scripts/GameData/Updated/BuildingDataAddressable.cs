using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SunnysideIsland.GameData
{
    /// <summary>
    /// Addressable을 지원하는 건설물 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class BuildingDataAddressable
    {
        public string buildingId;
        public string buildingName;
        public BuildingCategory category;
        public Vector2Int size;
        public float buildTime;
        public int instantCompleteCost;
        
        [TextArea(3, 5)]
        public string description;
        public string effectDescription;
        
        public List<BuildingCost> requiredResources;
        
        // 기존 string path (하위 호환성)
        public string prefabPath;
        public string iconPath;
        
        // Addressable 참조 (새로운 방식)
        public AssetReferenceGameObject prefabReference;
        public AssetReferenceT<Sprite> iconReference;
        
        // 상업 건설물용
        public int dailyIncomeMin;
        public int dailyIncomeMax;
        
        // 관광 건설물용
        public int touristIncrease;
        
        // 런타임 캐싱
        [System.NonSerialized] private GameObject _cachedPrefab;
        [System.NonSerialized] private Sprite _cachedIcon;
        
        /// <summary>
        /// 건설물 프리팹을 비동기로 로드합니다.
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
        /// 건설물을 인스턴스화합니다.
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
        
        /// <summary>
        /// Addressable 참조가 유효한지 확인합니다.
        /// </summary>
        public bool HasValidPrefabReference()
        {
            return prefabReference != null && prefabReference.RuntimeKeyIsValid();
        }
        
        public bool HasValidIconReference()
        {
            return iconReference != null && iconReference.RuntimeKeyIsValid();
        }
    }
    
    [System.Serializable]
    public class BuildingCost
    {
        public string itemId;
        public int quantity;
    }
    
    public enum BuildingCategory
    {
        Housing,
        Agriculture,
        Commerce,
        Tourism,
        Production,
        Decoration
    }
}
