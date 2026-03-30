using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using ExcelConverter.Core;

namespace ExcelConverter.Editor
{
    /// <summary>
    /// Excel Converter 유틸리티 메뉴
    /// </summary>
    public static class ExcelConverterMenu
    {
        [MenuItem("Assets/Excel/Convert to ScriptableObject", priority = 0)]
        private static void ConvertSelectedExcel()
        {
            var selectedPath = Selection.activeObject != null 
                ? AssetDatabase.GetAssetPath(Selection.activeObject) 
                : null;

            if (string.IsNullOrEmpty(selectedPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select an Excel or CSV file.", "OK");
                return;
            }

            var absolutePath = Path.Combine(Application.dataPath, "..", selectedPath);
            absolutePath = Path.GetFullPath(absolutePath);

            if (!File.Exists(absolutePath))
            {
                EditorUtility.DisplayDialog("Error", "File not found.", "OK");
                return;
            }

            var extension = Path.GetExtension(absolutePath).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
            {
                EditorUtility.DisplayDialog("Error", "Please select a .xlsx, .xls, or .csv file.", "OK");
                return;
            }

            // Open window with pre-filled path
            var window = EditorWindow.GetWindow<ExcelConverterWindow>("Excel Converter");
            window.Show();
            
            // Use reflection to set the path (simplified approach)
            Debug.Log($"[ExcelConverter] Selected file: {absolutePath}");
            Debug.Log("[ExcelConverter] Please select the GameData script and click CONVERT.");
        }

        [MenuItem("Assets/Excel/Convert to ScriptableObject", validate = true)]
        private static bool ConvertSelectedExcelValidation()
        {
            if (Selection.activeObject == null) return false;
            
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path)) return false;

            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".xlsx" || extension == ".xls" || extension == ".csv";
        }

        [MenuItem("Tools/Excel Converter/Documentation", priority = 100)]
        private static void OpenDocumentation()
        {
            var docPath = Path.Combine(Application.dataPath, "Scripts", "ExcelConverter", "README.md");
            if (File.Exists(docPath))
            {
                Application.OpenURL("file://" + docPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation", 
                    "Documentation not found. Please check the README.md file in Assets/Scripts/ExcelConverter/", 
                    "OK");
            }
        }
    }
}
