using UnityEngine;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// Campfire Fire Pool 초기화
    /// Campfire_Fire 오브젝트를 Pool에서 관리
    /// </summary>
    public class CampfireFirePoolInitializer : MonoBehaviour
    {
        [Header("=== Campfire Fire Pool Settings ===")]
        [SerializeField] private GameObject _campfireFirePrefab;
        [SerializeField] private string _poolName = "CampfireFire";
        [SerializeField] private int _initialSize = 5;
        [SerializeField] private int _maxSize = 20;
        
        private void Start()
        {
            InitializeCampfireFirePool();
        }
        
        private void InitializeCampfireFirePool()
        {
            // 프리팹이 할당되지 않았으면 Resources에서 찾기
            if (_campfireFirePrefab == null)
            {
                _campfireFirePrefab = Resources.Load<GameObject>("Prefabs/Campfire_Fire");
                if (_campfireFirePrefab == null)
                    _campfireFirePrefab = Resources.Load<GameObject>("Campfire_Fire");
                if (_campfireFirePrefab == null)
                    _campfireFirePrefab = Resources.Load<GameObject>("Building/Campfire_Fire");
                
                if (_campfireFirePrefab != null)
                {
                    Debug.Log($"[CampfireFirePoolInitializer] Loaded Campfire_Fire prefab from Resources: {_campfireFirePrefab.name}");
                }
            }
            
            if (_campfireFirePrefab == null)
            {
                Debug.LogError("[CampfireFirePoolInitializer] Campfire_Fire prefab is not assigned and not found in Resources!");
                return;
            }
            
            if (Pool.PoolManager.Instance != null)
            {
                // 풀이 이미 존재하는지 확인
                var existingPool = Pool.PoolManager.Instance.GetPool(_poolName);
                if (existingPool == null)
                {
                    // 새 풀 생성
                    Pool.PoolManager.Instance.CreatePool(_poolName, _campfireFirePrefab, _initialSize, _maxSize);
                    Debug.Log($"[CampfireFirePoolInitializer] CampfireFire pool created with initial size: {_initialSize}, max size: {_maxSize}");
                }
            }
            else
            {
                Debug.LogError("[CampfireFirePoolInitializer] PoolManager instance not found!");
            }
        }
    }
}
