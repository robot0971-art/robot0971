using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace SunnysideIsland.Pool
{
    public class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();
        private readonly HashSet<GameObject> _activeObjects = new HashSet<GameObject>();
        
        private int _maxSize;
        private int _initialSize;
        private bool _collectionCheck;
        
        public int CountAll => _pool.Count + _activeObjects.Count;
        public int CountActive => _activeObjects.Count;
        public int CountInactive => _pool.Count;
        public string PoolName { get; private set; }
        
        public ObjectPool(GameObject prefab, Transform parent = null, int initialSize = 10, int maxSize = 100, bool collectionCheck = true, string poolName = null)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
            _maxSize = maxSize;
            _collectionCheck = collectionCheck;
            PoolName = poolName ?? prefab.name;
            
            Prewarm();
        }
        
        private void Prewarm()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var obj = CreateNewObject();
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }
        
        private GameObject CreateNewObject()
        {
            var obj = Object.Instantiate(_prefab, _parent);
            obj.name = $"{_prefab.name}_pooled";
            
            var poolable = obj.GetComponent<PoolableObject>();
            if (poolable != null)
            {
                poolable.SetOwnerPool(this);
            }
            
            return obj;
        }
        
        public GameObject Get()
        {
            GameObject obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else if (CountAll < _maxSize)
            {
                obj = CreateNewObject();
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Pool '{PoolName}' reached max size {_maxSize}");
                return null;
            }
            
            obj.SetActive(true);
            _activeObjects.Add(obj);
            
            var poolable = obj.GetComponent<PoolableObject>();
            if (poolable != null)
            {
                poolable.OnSpawnFromPool();
            }
            
            return obj;
        }
        
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            var obj = Get();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }
        
        public T Get<T>() where T : Component
        {
            var obj = Get();
            return obj?.GetComponent<T>();
        }
        
        public T Get<T>(Vector3 position, Quaternion rotation) where T : Component
        {
            var obj = Get(position, rotation);
            return obj?.GetComponent<T>();
        }
        
        public void Return(GameObject obj)
        {
            if (obj == null) return;
            
            if (_collectionCheck && !_activeObjects.Contains(obj))
            {
                Debug.LogWarning($"[ObjectPool] Trying to return object that wasn't from this pool: {obj.name}");
                return;
            }
            
            var poolable = obj.GetComponent<PoolableObject>();
            if (poolable != null)
            {
                poolable.OnReturnToPool();
            }
            else
            {
                obj.SetActive(false);
            }
            
            if (_parent != null)
            {
                obj.transform.SetParent(_parent);
            }
            
            _activeObjects.Remove(obj);
            _pool.Enqueue(obj);
        }
        
        public void ReturnAll()
        {
            var tempList = new List<GameObject>(_activeObjects);
            foreach (var obj in tempList)
            {
                if (obj != null)
                {
                    Return(obj);
                }
            }
        }
        
        public void Clear()
        {
            foreach (var obj in _pool)
            {
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }
            
            foreach (var obj in _activeObjects)
            {
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }
            
            _pool.Clear();
            _activeObjects.Clear();
        }
        
        public void SetMaxSize(int maxSize)
        {
            _maxSize = maxSize;
        }
    }
}