using System.Collections.Generic;
using UnityEngine;
using DI;

namespace SunnysideIsland.Pool
{
    public interface IPoolManager
    {
        ObjectPool GetPool(string poolName);
        GameObject Spawn(string poolName);
        GameObject Spawn(string poolName, Vector3 position, Quaternion rotation);
        T Spawn<T>(string poolName) where T : Component;
        void Despawn(string poolName, GameObject obj);
        void DespawnAll(string poolName);
    }

    public class PoolManager : MonoBehaviour, IPoolManager
    {
        public static PoolManager Instance { get; private set; }
        
        [Header("=== Pool Settings ===")]
        [SerializeField] private int _defaultInitialSize = 10;
        [SerializeField] private int _defaultMaxSize = 100;
        [SerializeField] private bool _collectionCheck = true;
        
        [Header("=== Predefined Pools ===")]
        [SerializeField] private List<PoolConfig> _predefinedPools = new List<PoolConfig>();
        
        private readonly Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();
        private Transform _poolContainer;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            DIContainer.Global.RegisterInstance<IPoolManager>(this);
            
            _poolContainer = new GameObject("PoolContainer").transform;
            _poolContainer.SetParent(transform);
            
            InitializePredefinedPools();
        }
        
        private void InitializePredefinedPools()
        {
            foreach (var config in _predefinedPools)
            {
                if (config.Prefab != null)
                {
                    CreatePool(config.PoolName, config.Prefab, config.InitialSize, config.MaxSize);
                }
            }
        }
        
        public ObjectPool CreatePool(string poolName, GameObject prefab, int initialSize = -1, int maxSize = -1)
        {
            if (string.IsNullOrEmpty(poolName))
            {
                poolName = prefab.name;
            }
            
            if (_pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[PoolManager] Pool '{poolName}' already exists");
                return _pools[poolName];
            }
            
            var poolParent = new GameObject($"Pool_{poolName}").transform;
            poolParent.SetParent(_poolContainer);
            
            int size = initialSize > 0 ? initialSize : _defaultInitialSize;
            int max = maxSize > 0 ? maxSize : _defaultMaxSize;
            
            var pool = new ObjectPool(prefab, poolParent, size, max, _collectionCheck, poolName);
            _pools[poolName] = pool;
            
            return pool;
        }
        
        public ObjectPool GetPool(string poolName)
        {
            if (_pools.TryGetValue(poolName, out var pool))
            {
                return pool;
            }
            return null;
        }
        
        public GameObject Spawn(string poolName)
        {
            var pool = GetPool(poolName);
            if (pool == null)
            {
                Debug.LogError($"[PoolManager] Pool '{poolName}' not found");
                return null;
            }
            return pool.Get();
        }
        
        public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation)
        {
            var pool = GetPool(poolName);
            if (pool == null)
            {
                Debug.LogError($"[PoolManager] Pool '{poolName}' not found");
                return null;
            }
            return pool.Get(position, rotation);
        }
        
        public T Spawn<T>(string poolName) where T : Component
        {
            var obj = Spawn(poolName);
            return obj?.GetComponent<T>();
        }
        
        public T Spawn<T>(string poolName, Vector3 position, Quaternion rotation) where T : Component
        {
            var obj = Spawn(poolName, position, rotation);
            return obj?.GetComponent<T>();
        }
        
        public void Despawn(string poolName, GameObject obj)
        {
            var pool = GetPool(poolName);
            if (pool == null)
            {
                Debug.LogError($"[PoolManager] Pool '{poolName}' not found");
                return;
            }
            pool.Return(obj);
        }
        
        public void DespawnAll(string poolName)
        {
            var pool = GetPool(poolName);
            if (pool == null)
            {
                Debug.LogError($"[PoolManager] Pool '{poolName}' not found");
                return;
            }
            pool.ReturnAll();
        }
        
        public void ClearPool(string poolName)
        {
            var pool = GetPool(poolName);
            if (pool != null)
            {
                pool.Clear();
                _pools.Remove(poolName);
            }
        }
        
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
        }
        
        public List<string> GetPoolNames()
        {
            return new List<string>(_pools.Keys);
        }
        
        public int GetActiveCount(string poolName)
        {
            var pool = GetPool(poolName);
            return pool?.CountActive ?? 0;
        }
    }
    
    [System.Serializable]
    public class PoolConfig
    {
        public string PoolName;
        public GameObject Prefab;
        [Min(1)] public int InitialSize = 10;
        [Min(1)] public int MaxSize = 100;
    }
}