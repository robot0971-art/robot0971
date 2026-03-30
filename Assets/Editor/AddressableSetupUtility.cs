using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace SunnysideIsland.Editor
{
    /// <summary>
    /// Addressable 설정을 자동화하는 에디터 유틸리티
    /// </summary>
    public static class AddressableSetupUtility
    {
        private const string SettingsAssetPath = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
        
        [MenuItem("Tools/Addressables/Setup Addressable Groups")]
        public static void SetupAddressableGroups()
        {
            // Addressable 설정이 존재하는지 확인
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                CreateAddressableSettings();
                settings = AddressableAssetSettingsDefaultObject.Settings;
            }
            
            if (settings == null)
            {
                Debug.LogError("Failed to create Addressable settings!");
                return;
            }
            
            // 그룹 생성
            CreateGroup(settings, "Items", false);
            CreateGroup(settings, "Buildings", false);
            CreateGroup(settings, "Characters", false);
            CreateGroup(settings, "UI", false);
            CreateGroup(settings, "Effects", false);
            CreateGroup(settings, "Audio", false);
            CreateGroup(settings, "Scenes", false);
            
            AssetDatabase.SaveAssets();
            Debug.Log("Addressable groups created successfully!");
        }
        
        [MenuItem("Tools/Addressables/Auto Assign Addressables")]
        public static void AutoAssignAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable settings not found! Please run Setup Addressable Groups first.");
                return;
            }
            
            // Items 폴더의 에셋 등록
            AssignFolderToGroup(settings, "Assets/Prefabs/Items", "Items");
            AssignFolderToGroup(settings, "Assets/Sprites/Items", "Items");
            
            // Buildings 폴더의 에셋 등록
            AssignFolderToGroup(settings, "Assets/Prefabs/Buildings", "Buildings");
            
            // Characters 폴더의 에셋 등록
            AssignFolderToGroup(settings, "Assets/Prefabs/Characters", "Characters");
            AssignFolderToGroup(settings, "Assets/Prefabs/Enemies", "Characters");
            
            // UI 폴더의 에셋 등록
            AssignFolderToGroup(settings, "Assets/Prefabs/UI", "UI");
            AssignFolderToGroup(settings, "Assets/Sprites/UI", "UI");
            
            // Scenes 폴더의 에셋 등록
            AssignFolderToGroup(settings, "Assets/Scenes", "Scenes");
            
            AssetDatabase.SaveAssets();
            Debug.Log("Assets assigned to Addressable groups successfully!");
        }
        
        [MenuItem("Tools/Addressables/Build Addressables")]
        public static void BuildAddressables()
        {
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("Addressables built successfully!");
        }
        
        /// <summary>
        /// Addressable 설정을 생성합니다.
        /// </summary>
        private static void CreateAddressableSettings()
        {
            // AddressableAssetSettings 생성
            AddressableAssetSettings.Create(
                SettingsAssetPath, 
                "AddressableAssetSettings", 
                true, 
                true
            );
            
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// Addressable 그룹을 생성하거나 가져옵니다.
        /// </summary>
        private static AddressableAssetGroup CreateGroup(AddressableAssetSettings settings, string groupName, bool readOnly)
        {
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, readOnly, false, false, null);
                
                // 기본 스키마 추가
                var schema = group.AddSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            }
            
            return group;
        }
        
        /// <summary>
        /// 폴더 내의 모든 에셋을 그룹에 등록합니다.
        /// </summary>
        private static void AssignFolderToGroup(AddressableAssetSettings settings, string folderPath, string groupName)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogWarning($"Folder not found: {folderPath}");
                return;
            }
            
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                Debug.LogError($"Group not found: {groupName}");
                return;
            }
            
            // 폴더 내의 모든 에셋 검색
            string[] guids = AssetDatabase.FindAssets("t:Object", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                
                if (asset != null)
                {
                    // Addressable에 등록
                    string address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                    AddAssetToGroup(settings, group, guid, address);
                    
                    // 라벨 추가
                    string label = $"{groupName}_{System.IO.Path.GetFileName(folderPath)}";
                    AddLabelToAsset(settings, guid, label);
                }
            }
        }
        
        /// <summary>
        /// 에셋을 그룹에 추가합니다.
        /// </summary>
        private static void AddAssetToGroup(AddressableAssetSettings settings, AddressableAssetGroup group, string guid, string address)
        {
            var entry = settings.CreateOrMoveEntry(guid, group);
            if (entry != null)
            {
                entry.address = address;
            }
        }
        
        /// <summary>
        /// 에셋에 라벨을 추가합니다.
        /// </summary>
        private static void AddLabelToAsset(AddressableAssetSettings settings, string guid, string label)
        {
            // 라벨이 존재하지 않으면 생성
            if (!settings.GetLabels().Contains(label))
            {
                settings.AddLabel(label);
            }
            
            var entry = settings.FindAssetEntry(guid);
            if (entry != null)
            {
                entry.SetLabel(label, true, true);
            }
        }
        
        /// <summary>
        /// 특정 에셋을 Addressable로 등록합니다.
        /// </summary>
        public static void RegisterAsset(Object asset, string groupName, string address = null)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            
            var group = settings.FindGroup(groupName) ?? CreateGroup(settings, groupName, false);
            
            string assetAddress = address ?? System.IO.Path.GetFileNameWithoutExtension(path);
            AddAssetToGroup(settings, group, guid, assetAddress);
        }
    }
}
