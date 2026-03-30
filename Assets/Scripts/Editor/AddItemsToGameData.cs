using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using GD = SunnysideIsland.GameData;

namespace SunnysideIsland.Editor
{
    public static class AddItemsToGameData
    {
        [MenuItem("Tools/Add Items To GameData")]
        public static void Execute()
        {
            string potatoGuid = "5790871d326542e47b60ed0ed6ab8ff5";
            string woodGuid = "1f619268bdc7f3741a6e55a3d22f95aa";
            
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable settings not found!");
                return;
            }
            
            var group = settings.DefaultGroup;
            
            var potatoEntry = settings.CreateOrMoveEntry(potatoGuid, group);
            potatoEntry.address = "Items/potato";
            potatoEntry.labels.Add("Items");
            
            var woodEntry = settings.CreateOrMoveEntry(woodGuid, group);
            woodEntry.address = "Items/wood";
            woodEntry.labels.Add("Items");
            
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, potatoEntry, true);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, woodEntry, true);
            
            string gameDataPath = "Assets/ScriptableObjects/GameData/GameData.asset";
            GD.GameData gameData = AssetDatabase.LoadAssetAtPath<GD.GameData>(gameDataPath);
            
            if (gameData == null)
            {
                Debug.LogError("Failed to load GameData!");
                return;
            }
            
            var potatoItem = new GD.ItemData();
            potatoItem.itemId = "item_potato";
            potatoItem.itemName = "감자";
            potatoItem.itemType = GD.ItemType.Material;
            potatoItem.maxStack = 99;
            potatoItem.baseValue = 10;
            potatoItem.canSell = true;
            potatoItem.description = "신선한 감자입니다.";
            potatoItem.IconReference = new UnityEngine.AddressableAssets.AssetReference(potatoGuid);
            gameData.items.Add(potatoItem);
            
            var woodItem = new GD.ItemData();
            woodItem.itemId = "item_wood";
            woodItem.itemName = "나무";
            woodItem.itemType = GD.ItemType.Material;
            woodItem.maxStack = 99;
            woodItem.baseValue = 5;
            woodItem.canSell = true;
            woodItem.description = "기본적인 나무 재료입니다.";
            woodItem.IconReference = new UnityEngine.AddressableAssets.AssetReference(woodGuid);
            gameData.items.Add(woodItem);
            
            EditorUtility.SetDirty(gameData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("감자와 나무 아이템이 추가되었습니다!");
        }
    }
}