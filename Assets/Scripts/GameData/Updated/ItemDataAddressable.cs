using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SunnysideIsland.GameData
{
    /// <summary>
    /// Addressable을 지원하는 아이템 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class ItemDataAddressable
    {
        public string itemId;
        public string itemName;
        public ItemType itemType;
        public ItemRarity rarity;
        public int maxStack;
        public int buyPrice;
        public int sellPrice;
        
        [TextArea(3, 5)]
        public string description;
        
        // 기존 string path (하위 호환성)
        public string iconPath;
        public string prefabPath;
        
        // Addressable 참조 (새로운 방식)
        [SerializeField] 
        private AssetReference _iconReference;
        public AssetReference IconReference { get => _iconReference; set => _iconReference = value; }
        
        public AssetReferenceGameObject worldPrefabReference;
        
        // 런타임 캐싱
        [System.NonSerialized] private Sprite _cachedIcon;
        [System.NonSerialized] private GameObject _cachedPrefab;
        [System.NonSerialized] private bool _isIconLoading;
        [System.NonSerialized] private bool _isPrefabLoading;
        
        /// <summary>
        /// 아이콘을 비동기로 로드합니다.
        /// </summary>
        public async Task<Sprite> LoadIconAsync()
        {
            if (_cachedIcon != null)
                return _cachedIcon;
            
            if (_isIconLoading)
            {
                // 로딩 중이면 완료될 때까지 대기
                while (_isIconLoading)
                {
                    await Task.Yield();
                }
                return _cachedIcon;
            }
            
            _isIconLoading = true;
            
            try
            {
                if (_iconReference != null && _iconReference.RuntimeKeyIsValid())
                {
                    var handle = _iconReference.LoadAssetAsync<Sprite>();
                    _cachedIcon = await handle.Task;
                }
                else if (!string.IsNullOrEmpty(iconPath))
                {
                    // Fallback: Resources나 기존 방식 사용
                    _cachedIcon = Resources.Load<Sprite>(iconPath);
                }
            }
            finally
            {
                _isIconLoading = false;
            }
            
            return _cachedIcon;
        }
        
        /// <summary>
        /// 월드 프리팹을 비동기로 로드합니다.
        /// </summary>
        public async Task<GameObject> LoadPrefabAsync()
        {
            if (_cachedPrefab != null)
                return _cachedPrefab;
            
            if (_isPrefabLoading)
            {
                while (_isPrefabLoading)
                {
                    await Task.Yield();
                }
                return _cachedPrefab;
            }
            
            _isPrefabLoading = true;
            
            try
            {
                if (worldPrefabReference != null && worldPrefabReference.RuntimeKeyIsValid())
                {
                    var handle = worldPrefabReference.LoadAssetAsync<GameObject>();
                    _cachedPrefab = await handle.Task;
                }
                else if (!string.IsNullOrEmpty(prefabPath))
                {
                    _cachedPrefab = Resources.Load<GameObject>(prefabPath);
                }
            }
            finally
            {
                _isPrefabLoading = false;
            }
            
            return _cachedPrefab;
        }
        
        /// <summary>
        /// 월드 프리팹을 인스턴스화합니다.
        /// </summary>
        public async Task<GameObject> InstantiatePrefabAsync(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (worldPrefabReference != null && worldPrefabReference.RuntimeKeyIsValid())
            {
                var handle = worldPrefabReference.InstantiateAsync(position, rotation, parent);
                return await handle.Task;
            }
            else if (!string.IsNullOrEmpty(prefabPath))
            {
                var prefab = Resources.Load<GameObject>(prefabPath);
                if (prefab != null)
                {
                    return Object.Instantiate(prefab, position, rotation, parent);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 캐시된 에셋을 해제합니다.
        /// </summary>
        public void ReleaseAssets()
        {
            if (_cachedIcon != null && _iconReference != null)
            {
                _iconReference.ReleaseAsset();
                _cachedIcon = null;
            }
            
            if (_cachedPrefab != null && worldPrefabReference != null)
            {
                worldPrefabReference.ReleaseAsset();
                _cachedPrefab = null;
            }
        }
        
        /// <summary>
        /// 동기적으로 아이콘을 가져옵니다 (이미 로드된 경우만).
        /// </summary>
        public Sprite GetIcon()
        {
            return _cachedIcon;
        }
        
        /// <summary>
        /// Addressable 참조가 유효한지 확인합니다.
        /// </summary>
        public bool HasValidIconReference()
        {
            return _iconReference != null && _iconReference.RuntimeKeyIsValid();
        }
        
        public bool HasValidPrefabReference()
        {
            return worldPrefabReference != null && worldPrefabReference.RuntimeKeyIsValid();
        }
    }
}
