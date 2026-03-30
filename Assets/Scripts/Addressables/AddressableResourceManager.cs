using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using SunnysideIsland.AddressableManagement;

// Addressables 클래스와 구분을 위해 alias 사용
using Addr = UnityEngine.AddressableAssets.Addressables;

namespace SunnysideIsland.ResourceManagement
{
    public class AddressableResourceManager : MonoBehaviour
    {
        public static AddressableResourceManager Instance { get; private set; }
        
        private Dictionary<string, object> _loadedAssets = new Dictionary<string, object>();
        private Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();
        
        [Header("=== Preload Settings ===")]
        [SerializeField] private List<AssetReference> _preloadAssets = new List<AssetReference>();
        
        [Header("=== Debug ===")]
        [SerializeField] private bool _showDebugLogs = false;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Log("AddressableResourceManager initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                ReleaseAllAssets();
                Instance = null;
            }
        }
        
        /// <summary>
        /// 비동기로 에셋을 로드합니다.
        /// </summary>
        public async Task<T> LoadAssetAsync<T>(AssetReference assetReference) where T : Object
        {
            if (assetReference == null || string.IsNullOrEmpty(assetReference.AssetGUID))
            {
                LogWarning("Invalid asset reference");
                return null;
            }
            
            string key = assetReference.AssetGUID;
            
            // 캐시 확인
            if (_loadedAssets.TryGetValue(key, out object cached))
            {
                Log($"Asset found in cache: {key}");
                return cached as T;
            }
            
            // 로드
            Log($"Loading asset: {key}");
            var handle = Addressables.LoadAssetAsync<T>(assetReference);
            
            try
            {
                T asset = await handle.Task;
                
                if (asset != null)
                {
                    _loadedAssets[key] = asset;
                    _handles[key] = handle;
                    Log($"Asset loaded successfully: {key}");
                }
                else
                {
                    LogWarning($"Failed to load asset: {key}");
                }
                
                return asset;
            }
            catch (System.Exception ex)
            {
                LogError($"Error loading asset {key}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// AssetReferenceT<T>를 사용하여 비동기로 에셋을 로드합니다.
        /// </summary>
        public async Task<T> LoadAssetAsync<T>(AssetReferenceT<T> assetReference) where T : Object
        {
            if (assetReference == null || string.IsNullOrEmpty(assetReference.AssetGUID))
            {
                LogWarning("Invalid asset reference");
                return null;
            }
            
            string key = assetReference.AssetGUID;
            
            if (_loadedAssets.TryGetValue(key, out object cached))
            {
                return cached as T;
            }
            
            var handle = assetReference.LoadAssetAsync();
            
            try
            {
                T asset = await handle.Task;
                
                if (asset != null)
                {
                    _loadedAssets[key] = asset;
                    _handles[key] = handle;
                }
                
                return asset;
            }
            catch (System.Exception ex)
            {
                LogError($"Error loading asset {key}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 프리팹을 비동기로 인스턴스화합니다.
        /// </summary>
        public async Task<GameObject> InstantiatePrefabAsync(
            AssetReferenceGameObject prefabReference, 
            Vector3 position, 
            Quaternion rotation, 
            Transform parent = null)
        {
            if (prefabReference == null || string.IsNullOrEmpty(prefabReference.AssetGUID))
            {
                LogWarning("Invalid prefab reference");
                return null;
            }
            
            Log($"Instantiating prefab: {prefabReference.AssetGUID}");
            
            var handle = prefabReference.InstantiateAsync(position, rotation, parent);
            
            try
            {
                GameObject instance = await handle.Task;
                
                if (instance != null)
                {
                    // Addressable 인스턴스 트래커 추가
                    var tracker = instance.AddComponent<AddressableInstanceTracker>();
                    tracker.Initialize(prefabReference.AssetGUID);
                    Log($"Prefab instantiated: {instance.name}");
                }
                
                return instance;
            }
            catch (System.Exception ex)
            {
                LogError($"Error instantiating prefab: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 프리팹을 부모 없이 인스턴스화합니다.
        /// </summary>
        public async Task<GameObject> InstantiatePrefabAsync(
            AssetReferenceGameObject prefabReference,
            Transform parent = null)
        {
            return await InstantiatePrefabAsync(prefabReference, Vector3.zero, Quaternion.identity, parent);
        }
        
        /// <summary>
        /// 라벨로 일괄 로드를 수행합니다.
        /// </summary>
        public async Task PreloadAssetsByLabel(string label)
        {
            Log($"Preloading assets with label: {label}");
            
            var locationsHandle = Addressables.LoadResourceLocationsAsync(label);
            var locations = await locationsHandle.Task;
            
            var loadTasks = new List<Task>();
            foreach (var location in locations)
            {
                loadTasks.Add(LoadByLocation(location));
            }
            
            await Task.WhenAll(loadTasks);
            Addressables.Release(locationsHandle);
            
            Log($"Preloaded {locations.Count} assets with label: {label}");
        }
        
        private async Task LoadByLocation(IResourceLocation location)
        {
            string key = location.PrimaryKey;
            
            if (_loadedAssets.ContainsKey(key))
            {
                return;
            }
            
            var handle = Addressables.LoadAssetAsync<Object>(location);
            
            try
            {
                Object asset = await handle.Task;
                
                if (asset != null)
                {
                    _loadedAssets[key] = asset;
                    _handles[key] = handle;
                }
            }
            catch
            {
                // 로드 실패 시 무시
            }
        }
        
        /// <summary>
        /// 특정 에셋을 해제합니다.
        /// </summary>
        public void ReleaseAsset(AssetReference assetReference)
        {
            if (assetReference == null) return;
            
            string key = assetReference.AssetGUID;
            ReleaseAssetByKey(key);
        }
        
        /// <summary>
        /// 키로 에셋을 해제합니다.
        /// </summary>
        public void ReleaseAssetByKey(string key)
        {
            if (_handles.TryGetValue(key, out AsyncOperationHandle handle))
            {
                Addressables.Release(handle);
                _handles.Remove(key);
                _loadedAssets.Remove(key);
                Log($"Asset released: {key}");
            }
        }
        
        /// <summary>
        /// 모든 로드된 에셋을 해제합니다.
        /// </summary>
        public void ReleaseAllAssets()
        {
            Log($"Releasing all assets. Count: {_handles.Count}");
            
            foreach (var handle in _handles.Values)
            {
                Addressables.Release(handle);
            }
            
            _handles.Clear();
            _loadedAssets.Clear();
        }
        
        /// <summary>
        /// 초기 프리로드 에셋들을 로드합니다.
        /// </summary>
        public async Task PreloadInitialAssets()
        {
            Log($"Preloading {_preloadAssets.Count} initial assets");
            
            var tasks = new List<Task>();
            foreach (var assetRef in _preloadAssets)
            {
                if (assetRef != null)
                {
                    tasks.Add(LoadAssetAsync<Object>(assetRef));
                }
            }
            
            await Task.WhenAll(tasks);
            Log("Initial assets preloaded");
        }
        
        /// <summary>
        /// 캐시된 에셋인지 확인합니다.
        /// </summary>
        public bool IsAssetCached(AssetReference assetReference)
        {
            if (assetReference == null) return false;
            return _loadedAssets.ContainsKey(assetReference.AssetGUID);
        }
        
        /// <summary>
        /// 로드된 에셋 개수를 반환합니다.
        /// </summary>
        public int GetLoadedAssetCount()
        {
            return _loadedAssets.Count;
        }
        
        #region Debug Helpers
        
        private void Log(string message)
        {
            if (_showDebugLogs)
            {
                Debug.Log($"[AddressableResourceManager] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[AddressableResourceManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[AddressableResourceManager] {message}");
        }
        
        #endregion
    }
}
