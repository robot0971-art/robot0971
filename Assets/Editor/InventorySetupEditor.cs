using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SunnysideIsland.UI.Inventory;
using SunnysideIsland.Inventory;
using SunnysideIsland.Farming;
using SunnysideIsland.GameData;

public class InventorySetupEditor : EditorWindow
{
    [MenuItem("Tools/Setup Inventory")]
    public static void ShowWindow()
    {
        SetupInventory();
    }

    [MenuItem("Tools/Setup Inventory")]
    public static void SetupInventory()
    {
        var scene = EditorSceneManager.GetActiveScene();
        
        // 1. Find or create objects
        var inventorySystem = FindOrCreate<InventorySystem>("InventorySystem");
        var farmingManager = FindOrCreate<FarmingManager>("FarmingManager");
        
        // Load GameData from project
        var gameData = AssetDatabase.LoadAssetAtPath<GameData>("Assets/ScriptableObjects/GameData/GameData.asset");
        
        // 2. Setup InventoryPanel
        var inventoryPanelGO = GameObject.Find("InventoryPanel");
        if (inventoryPanelGO != null)
        {
            var inventoryPanel = inventoryPanelGO.GetComponent<InventoryPanel>();
            if (inventoryPanel == null)
            {
                inventoryPanel = inventoryPanelGO.AddComponent<InventoryPanel>();
            }
            
            // Set fields via serialized object
            var so = new SerializedObject(inventoryPanel);
            
            // _inventoryGrid
            var slotGrid = GameObject.Find("SlotGrid");
            if (slotGrid != null)
            {
                so.FindProperty("_inventoryGrid").objectReferenceValue = slotGrid.transform;
            }
            
            // _inventory
            so.FindProperty("_inventory").objectReferenceValue = inventorySystem;
            
            // _gameData
            so.FindProperty("_gameData").objectReferenceValue = gameData;
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(inventoryPanel);
        }
        
        // 3. Setup FarmingManager
        if (farmingManager != null)
        {
            var so = new SerializedObject(farmingManager);
            so.FindProperty("_inventorySystem").objectReferenceValue = inventorySystem;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(farmingManager);
        }
        
        // 4. Save scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        
        Debug.Log("[InventorySetup] Setup complete!");
    }
    
    private static T FindOrCreate<T>(string name) where T : MonoBehaviour
    {
        var obj = FindObjectOfType<T>();
        if (obj == null)
        {
            var go = new GameObject(name);
            obj = go.AddComponent<T>();
            EditorUtility.SetDirty(go);
        }
        return obj;
    }
}
