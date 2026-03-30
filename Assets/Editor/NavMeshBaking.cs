using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

namespace SunnysideIsland.Tools
{
    public class NavMeshBaking
    {
        [MenuItem("SunnysideIsland/Bake NavMesh")]
        public static void BakeNavMesh()
        {
            var ground = GameObject.Find("Ground");
            if (ground == null)
            {
                Debug.LogError("[NavMeshBaking] Ground object not found!");
                return;
            }

            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic);
            foreach (Transform child in ground.transform)
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.NavigationStatic);
            }

            var tilemaps = Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None);
            foreach (var tilemap in tilemaps)
            {
                if (tilemap.gameObject.name.Contains("Ground"))
                {
                    GameObjectUtility.SetStaticEditorFlags(tilemap.gameObject, StaticEditorFlags.NavigationStatic);
                }
            }

            EditorApplication.ExecuteMenuItem("Window/AI/Navigation");

            var assembly = typeof(EditorWindow).Assembly;
            var navMeshEditorType = assembly.GetType("UnityEditor.AI.NavMeshEditorWindow");
            
            if (navMeshEditorType != null)
            {
                var window = EditorWindow.GetWindow(navMeshEditorType);
                if (window != null)
                {
                    window.SendEvent(EditorGUIUtility.CommandEvent("NavMeshEditorBake"));
                    Debug.Log("[NavMeshBaking] Bake command sent");
                }
            }
            else
            {
                Debug.LogWarning("[NavMeshBaking] NavMeshEditorWindow type not found. Please click Bake manually.");
            }

            var navData = NavMesh.CalculateTriangulation();
            Debug.Log($"[NavMeshBaking] NavMesh vertices after bake: {navData.vertices.Length}");
        }

        [MenuItem("SunnysideIsland/Bake NavMesh", true)]
        private static bool ValidateBakeNavMesh()
        {
            return !EditorApplication.isPlaying;
        }
    }
}