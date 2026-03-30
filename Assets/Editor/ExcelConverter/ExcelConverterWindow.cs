using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ExcelConverter.Core;
using ExcelConverter.Interfaces;

namespace ExcelConverter.Editor
{
    /// <summary>
    /// Excel Converter Editor Window
    /// </summary>
    public class ExcelConverterWindow : EditorWindow
    {
        private string _excelFilePath = "";
        private string _outputPath = "Assets/Resources";
        private string _assetName = "GameData";
        private MonoScript _gameDataScript;
        private Vector2 _scrollPosition;
        private string _lastError = "";
        private string _lastSuccess = "";

        [MenuItem("Tools/Excel Converter/Open Window")]
        public static void ShowWindow()
        {
            GetWindow<ExcelConverterWindow>("Excel Converter");
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label("Excel to ScriptableObject Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Excel File Selection
            EditorGUILayout.LabelField("Excel File", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _excelFilePath = EditorGUILayout.TextField(_excelFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFilePanel(
                    "Select Excel File",
                    "",
                    "xlsx,xls,csv");
                if (!string.IsNullOrEmpty(path))
                {
                    _excelFilePath = path;
                    // Auto-set asset name from file name
                    _assetName = Path.GetFileNameWithoutExtension(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            // GameData Script Selection
            EditorGUILayout.LabelField("GameData Script (ScriptableObject)", EditorStyles.boldLabel);
            _gameDataScript = EditorGUILayout.ObjectField(
                _gameDataScript, 
                typeof(MonoScript), 
                false) as MonoScript;
            EditorGUILayout.Space(5);

            // Output Path
            EditorGUILayout.LabelField("Output Path", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _outputPath = EditorGUILayout.TextField(_outputPath);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel(
                    "Select Output Folder",
                    _outputPath,
                    "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert absolute path to relative path
                    _outputPath = GetRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            // Asset Name
            EditorGUILayout.LabelField("Asset Name", EditorStyles.boldLabel);
            _assetName = EditorGUILayout.TextField(_assetName);
            EditorGUILayout.Space(10);

            // Convert Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CONVERT", GUILayout.Height(40)))
            {
                ConvertExcel();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(10);

            // Status Messages
            if (!string.IsNullOrEmpty(_lastError))
            {
                EditorGUILayout.HelpBox(_lastError, MessageType.Error);
            }
            if (!string.IsNullOrEmpty(_lastSuccess))
            {
                EditorGUILayout.HelpBox(_lastSuccess, MessageType.Info);
            }

            EditorGUILayout.Space(20);
            GUILayout.Label("Instructions", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "1. Select your Excel file (.xlsx or .csv)\n" +
                "2. Select your GameData script (must inherit ScriptableObject)\n" +
                "3. Set output path and asset name\n" +
                "4. Click CONVERT button\n\n" +
                "Note: ExcelDataReader package must be installed.\n" +
                "See PACKAGE_DEPENDENCIES.md for setup.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndScrollView();
        }

        private void ConvertExcel()
        {
            _lastError = "";
            _lastSuccess = "";

            // Validation
            if (string.IsNullOrEmpty(_excelFilePath))
            {
                _lastError = "Please select an Excel file.";
                return;
            }

            if (!File.Exists(_excelFilePath))
            {
                _lastError = $"File not found: {_excelFilePath}";
                return;
            }

            if (_gameDataScript == null)
            {
                _lastError = "Please select a GameData script.";
                return;
            }

            // Get GameData type
            var gameDataType = GetGameDataType();
            if (gameDataType == null)
            {
                _lastError = "Selected script is not a valid ScriptableObject type.";
                return;
            }

            if (!gameDataType.IsSubclassOf(typeof(ScriptableObject)))
            {
                _lastError = "Selected script must inherit from ScriptableObject.";
                return;
            }

            // Create output directory if not exists
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }

            // Convert using reflection to call generic method
            try
            {
                var converterType = typeof(ExcelConverter<>).MakeGenericType(gameDataType);
                var converter = Activator.CreateInstance(converterType);
                var convertMethod = converterType.GetMethod("Convert", new[] { typeof(string) });
                var result = convertMethod.Invoke(converter, new object[] { _excelFilePath });

                // Save as ScriptableObject
                var assetPath = Path.Combine(_outputPath, _assetName + ".asset");
                assetPath = assetPath.Replace("\\", "/");

                // Remove existing asset
                var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (existingAsset != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                AssetDatabase.CreateAsset((ScriptableObject)result, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                _lastSuccess = $"Successfully converted and saved to: {assetPath}";
                Debug.Log($"[ExcelConverter] Converted {_excelFilePath} to {assetPath}");
            }
            catch (TargetInvocationException tie)
            {
                _lastError = $"Conversion failed: {tie.InnerException?.Message ?? tie.Message}";
                Debug.LogException(tie);
            }
            catch (Exception ex)
            {
                _lastError = $"Conversion failed: {ex.Message}";
                Debug.LogException(ex);
            }
        }

        private Type GetGameDataType()
        {
            if (_gameDataScript == null) return null;
            return _gameDataScript.GetClass();
        }

        private string GetRelativePath(string absolutePath)
        {
            var projectPath = Application.dataPath;
            projectPath = projectPath.Substring(0, projectPath.Length - 6); // Remove "Assets"

            if (absolutePath.StartsWith(projectPath))
            {
                return "Assets" + absolutePath.Substring(projectPath.Length);
            }
            return absolutePath;
        }
    }
}
