using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SunnysideIsland.AddressableManagement
{
    /// <summary>
    /// 게임 데이터를 로드하고 관리하는 클래스
    /// </summary>
    public class GameDataLoader : MonoBehaviour
    {
        public static GameDataLoader Instance { get; private set; }
        
        [Header("=== GameData Reference ===")]
        [SerializeField] private AssetReferenceT<ScriptableObject> _gameDataReference;
        
        private ScriptableObject _gameData;
        private bool _isGameDataLoaded;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// GameData를 비동기로 로드합니다.
        /// </summary>
        public async Task<ScriptableObject> LoadGameDataAsync()
        {
            if (_isGameDataLoaded && _gameData != null)
                return _gameData;
            
            if (_gameDataReference != null && _gameDataReference.RuntimeKeyIsValid())
            {
                var handle = _gameDataReference.LoadAssetAsync<ScriptableObject>();
                _gameData = await handle.Task;
                _isGameDataLoaded = true;
            }
            
            return _gameData;
        }
        
        /// <summary>
        /// 아이템 데이터를 로드합니다.
        /// </summary>
        public async Task<GameData.ItemDataAddressable> LoadItemData(string itemId)
        {
            var gameData = await LoadGameDataAsync();
            if (gameData == null) return null;
            
            // GameData에서 아이템 찾기
            // 이 부분은 실제 GameData 구조에 따라 수정 필요
            var item = GetItemFromGameData(gameData, itemId);
            if (item != null)
            {
                // 아이콘 미리 로드
                await item.LoadIconAsync();
            }
            
            return item;
        }
        
        /// <summary>
        /// 건설물을 인스턴스화합니다.
        /// </summary>
        public async Task<GameObject> CreateBuilding(string buildingId, Vector3 position)
        {
            var gameData = await LoadGameDataAsync();
            if (gameData == null) return null;
            
            var building = GetBuildingFromGameData(gameData, buildingId);
            if (building == null)
            {
                Debug.LogError($"Building not found: {buildingId}");
                return null;
            }
            
            return await building.InstantiateAsync(position, Quaternion.identity);
        }
        
        /// <summary>
        /// 몬스터를 스폰합니다.
        /// </summary>
        public async Task<GameObject> SpawnMonster(string monsterId, Vector3 position)
        {
            var gameData = await LoadGameDataAsync();
            if (gameData == null) return null;
            
            var monster = GetMonsterFromGameData(gameData, monsterId);
            if (monster == null)
            {
                Debug.LogWarning($"Monster not found: {monsterId}");
                return null;
            }
            
            return await monster.InstantiateAsync(position, Quaternion.identity);
        }
        
        /// <summary>
        /// 무기를 인스턴스화합니다.
        /// </summary>
        public async Task<GameObject> CreateWeapon(string weaponId, Vector3 position, Transform parent = null)
        {
            var gameData = await LoadGameDataAsync();
            if (gameData == null) return null;
            
            var weapon = GetWeaponFromGameData(gameData, weaponId);
            if (weapon == null)
            {
                Debug.LogWarning($"Weapon not found: {weaponId}");
                return null;
            }
            
            return await weapon.InstantiateWeaponAsync(position, Quaternion.identity, parent);
        }
        
        #region GameData Access Methods
        
        // 아래 메서드들은 실제 GameData 클래스 구조에 따라 구현 필요
        
        private GameData.ItemDataAddressable GetItemFromGameData(ScriptableObject gameData, string itemId)
        {
            // 실제 구현 필요
            // 예: return (gameData as GameDataContainer).GetItem(itemId);
            return null;
        }
        
        private GameData.BuildingDataAddressable GetBuildingFromGameData(ScriptableObject gameData, string buildingId)
        {
            // 실제 구현 필요
            return null;
        }
        
        private GameData.MonsterDataAddressable GetMonsterFromGameData(ScriptableObject gameData, string monsterId)
        {
            // 실제 구현 필요
            return null;
        }
        
        private GameData.WeaponDataAddressable GetWeaponFromGameData(ScriptableObject gameData, string weaponId)
        {
            // 실제 구현 필요
            return null;
        }
        
        #endregion
    }
}
