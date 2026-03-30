using UnityEngine;

namespace SunnysideIsland.Pool
{
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }

    [RequireComponent(typeof(PoolableObject))]
    public abstract class PoolableObject : MonoBehaviour, IPoolable
    {
        protected bool _isPooled = false;
        protected ObjectPool _ownerPool;

        public bool IsPooled => _isPooled;

        public void SetOwnerPool(ObjectPool pool)
        {
            _ownerPool = pool;
        }

        public virtual void OnSpawnFromPool()
        {
            _isPooled = false;
            gameObject.SetActive(true);
        }

        public virtual void OnReturnToPool()
        {
            _isPooled = true;
            gameObject.SetActive(false);
        }

        public void ReturnToPool()
        {
            if (_ownerPool != null)
            {
                _ownerPool.Return(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}