using UnityEngine;
using SunnysideIsland.Pool;
using SunnysideIsland.Items;

namespace SunnysideIsland.Animal
{
    public class EggPoint : MonoBehaviour
    {
        [Header("=== Egg Settings ===")]
        [SerializeField] private GameObject _eggPrefab;
        [SerializeField] private float _eggSpawnChance = 0.7f;
        [SerializeField] private string _poolName = "Egg";
        
        [Header("=== Visual Debug ===")]
        [SerializeField] private bool _showGizmo = true;
        [SerializeField] private float _gizmoRadius = 0.3f;
        
        public bool HasEgg { get; private set; } = false;
        public GameObject CurrentEgg { get; private set; } = null;
        
        private ObjectPool _eggPool;
        
        private void Start()
        {
            // 풀 참조 가져오기
            if (PoolManager.Instance != null)
            {
                _eggPool = PoolManager.Instance.GetPool(_poolName);
            }
            
            // 풀도 없고 프리팹도 없으면 Resources에서 찾기
            if (_eggPool == null && _eggPrefab == null)
            {
                _eggPrefab = Resources.Load<GameObject>("Prefabs/Egg");
                if (_eggPrefab == null)
                    _eggPrefab = Resources.Load<GameObject>("Egg");
                Debug.Log($"[EggPoint] {name} Loaded egg prefab from Resources: {_eggPrefab}");
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!_showGizmo) return;
            
            Gizmos.color = HasEgg ? Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(transform.position, _gizmoRadius);
            
            // 레이블 표시
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                HasEgg ? "Has Egg" : "Empty");
            #endif
        }
        
        public bool TrySpawnEgg()
        {
            if (HasEgg || CurrentEgg != null) return false;
            
            Debug.Log($"[EggPoint] {name} TrySpawnEgg called - Pool: {_eggPool}, Prefab: {_eggPrefab}");
            
            if (Random.value <= _eggSpawnChance)
            {
                SpawnEgg();
                return true;
            }
            
            return false;
        }
        
        private void SpawnEgg()
        {
            // 풀에서 Egg 가져오기
            if (_eggPool != null)
            {
                Debug.Log($"[EggPoint] {name} SpawnEgg from pool");
                CurrentEgg = _eggPool.Get();
                if (CurrentEgg != null)
                {
                    CurrentEgg.transform.position = transform.position;
                    CurrentEgg.transform.rotation = Quaternion.identity;
                    CurrentEgg.transform.SetParent(transform);
                    
                    // EggItem에 EggPoint 참조 설정
                    var eggItem = CurrentEgg.GetComponent<EggItem>();
                    if (eggItem != null)
                    {
                        eggItem.SetParentEggPoint(this);
                    }
                    
                    HasEgg = true;
                    Debug.Log($"[EggPoint] {name} Egg spawned from pool!");
                }
                else
                {
                    Debug.LogWarning($"[EggPoint] {name} Pool.Get() returned null!");
                }
            }
            else if (_eggPrefab != null)
            {
                // 풀이 없으면 Instantiate (fallback)
                Debug.Log($"[EggPoint] {name} SpawnEgg from prefab");
                CurrentEgg = Instantiate(_eggPrefab, transform.position, Quaternion.identity, transform);
                var eggItem = CurrentEgg.GetComponent<EggItem>();
                if (eggItem != null)
                {
                    eggItem.SetParentEggPoint(this);
                }
                HasEgg = true;
                Debug.Log($"[EggPoint] {name} Egg spawned from prefab!");
            }
            else
            {
                Debug.LogWarning($"[EggPoint] {name} No pool and no prefab! Cannot spawn egg.");
            }
        }
        
        public void CollectEgg()
        {
            if (CurrentEgg != null)
            {
                // 풀에 반환
                var eggItem = CurrentEgg.GetComponent<EggItem>();
                if (eggItem != null)
                {
                    eggItem.ReturnToPool();
                }
                else
                {
                    Destroy(CurrentEgg);
                }
                
                CurrentEgg = null;
                HasEgg = false;
            }
        }
        
        public void OnEggCollected()
        {
            HasEgg = false;
            CurrentEgg = null;
        }
    }
}
