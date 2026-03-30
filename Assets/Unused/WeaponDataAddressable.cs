using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SunnysideIsland.GameData
{
    /// <summary>
    /// Addressable을 지원하는 무기 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class WeaponDataAddressable
    {
        public string weaponId;
        public string weaponName;
        public int attackPower;
        public float attackSpeed;
        public AttackRangeType rangeType;
        public string specialEffect;
        public string recipeId;
        public string itemId;
        
        // 기존 string path (하위 호환성)
        public string prefabPath;
        public string projectilePrefabPath;
        public string animationClipPath;
        
        // Addressable 참조 (새로운 방식)
        public AssetReferenceGameObject weaponPrefabReference;
        public AssetReferenceGameObject projectilePrefabReference; // 원거리 무기용
        public AssetReferenceT<AnimationClip> attackAnimationReference;
        
        // 런타임 캐싱
        [System.NonSerialized] private GameObject _cachedWeaponPrefab;
        [System.NonSerialized] private GameObject _cachedProjectilePrefab;
        [System.NonSerialized] private AnimationClip _cachedAnimation;
        
        /// <summary>
        /// 무기 프리팹을 비동기로 로드합니다.
        /// </summary>
        public async Task<GameObject> LoadWeaponPrefabAsync()
        {
            if (_cachedWeaponPrefab != null)
                return _cachedWeaponPrefab;
            
            if (weaponPrefabReference != null && weaponPrefabReference.RuntimeKeyIsValid())
            {
                var handle = weaponPrefabReference.LoadAssetAsync<GameObject>();
                _cachedWeaponPrefab = await handle.Task;
            }
            else if (!string.IsNullOrEmpty(prefabPath))
            {
                _cachedWeaponPrefab = Resources.Load<GameObject>(prefabPath);
            }
            
            return _cachedWeaponPrefab;
        }
        
        /// <summary>
        /// 투사체 프리팹을 비동기로 로드합니다 (원거리 무기용).
        /// </summary>
        public async Task<GameObject> LoadProjectilePrefabAsync()
        {
            if (_cachedProjectilePrefab != null)
                return _cachedProjectilePrefab;
            
            if (projectilePrefabReference != null && projectilePrefabReference.RuntimeKeyIsValid())
            {
                var handle = projectilePrefabReference.LoadAssetAsync<GameObject>();
                _cachedProjectilePrefab = await handle.Task;
            }
            else if (!string.IsNullOrEmpty(projectilePrefabPath))
            {
                _cachedProjectilePrefab = Resources.Load<GameObject>(projectilePrefabPath);
            }
            
            return _cachedProjectilePrefab;
        }
        
        /// <summary>
        /// 공격 애니메이션 클립을 비동기로 로드합니다.
        /// </summary>
        public async Task<AnimationClip> LoadAnimationAsync()
        {
            if (_cachedAnimation != null)
                return _cachedAnimation;
            
            if (attackAnimationReference != null && attackAnimationReference.RuntimeKeyIsValid())
            {
                var handle = attackAnimationReference.LoadAssetAsync<AnimationClip>();
                _cachedAnimation = await handle.Task;
            }
            else if (!string.IsNullOrEmpty(animationClipPath))
            {
                _cachedAnimation = Resources.Load<AnimationClip>(animationClipPath);
            }
            
            return _cachedAnimation;
        }
        
        /// <summary>
        /// 무기를 인스턴스화합니다.
        /// </summary>
        public async Task<GameObject> InstantiateWeaponAsync(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (weaponPrefabReference != null && weaponPrefabReference.RuntimeKeyIsValid())
            {
                var handle = weaponPrefabReference.InstantiateAsync(position, rotation, parent);
                return await handle.Task;
            }
            else
            {
                var prefab = await LoadWeaponPrefabAsync();
                if (prefab != null)
                {
                    return Object.Instantiate(prefab, position, rotation, parent);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 투사체를 인스턴스화합니다.
        /// </summary>
        public async Task<GameObject> InstantiateProjectileAsync(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (projectilePrefabReference != null && projectilePrefabReference.RuntimeKeyIsValid())
            {
                var handle = projectilePrefabReference.InstantiateAsync(position, rotation, parent);
                return await handle.Task;
            }
            else
            {
                var prefab = await LoadProjectilePrefabAsync();
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
            if (_cachedWeaponPrefab != null && weaponPrefabReference != null)
            {
                weaponPrefabReference.ReleaseAsset();
                _cachedWeaponPrefab = null;
            }
            
            if (_cachedProjectilePrefab != null && projectilePrefabReference != null)
            {
                projectilePrefabReference.ReleaseAsset();
                _cachedProjectilePrefab = null;
            }
            
            if (_cachedAnimation != null && attackAnimationReference != null)
            {
                attackAnimationReference.ReleaseAsset();
                _cachedAnimation = null;
            }
        }
    }
    
    public enum AttackRangeType
    {
        Melee,
        Ranged,
        Magic
    }
}
