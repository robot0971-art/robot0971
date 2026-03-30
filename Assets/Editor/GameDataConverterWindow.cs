using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ExcelConverter.Attributes;
using ExcelConverter.Core;
using ExcelConverter.Exceptions;
using SunnysideIsland.GameData;
using GameDataContainer = SunnysideIsland.GameData.GameData;

namespace SunnysideIsland.Editor
{
    /// <summary>
    /// Sunnyside Island GameData 일괄 변환 Editor Window
    /// 모든 Excel 파일을 하나의 버튼으로 ScriptableObject로 변환합니다.
    /// </summary>
    public class GameDataConverterWindow : EditorWindow
    {
        private string _excelFolderPath = "GameData_Excel";
        private string _outputPath = "Assets/ScriptableObjects/GameData";
        private Vector2 _scrollPosition;
        private List<ConversionLog> _logs = new List<ConversionLog>();
        private bool _isConverting = false;
        private float _progress = 0f;
        private string _currentTask = "";
        
        private struct ConversionLog
        {
            public LogType Type;
            public string Message;
            public string Time;
            
            public ConversionLog(LogType type, string message)
            {
                Type = type;
                Message = message;
                Time = System.DateTime.Now.ToString("HH:mm:ss");
            }
        }
        
        private enum LogType { Info, Success, Warning, Error }
        
        // Excel 파일 매핑 정의
        private readonly List<ExcelMapping> _excelMappings = new List<ExcelMapping>
        {
            new ExcelMapping("01_Items.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Items", typeof(ItemData), "items")
            }),
            new ExcelMapping("02_Crops.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Crops", typeof(CropData), "crops")
            }),
            new ExcelMapping("03_Fishing.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Fish", typeof(FishData), "fishData"),
                new SheetMapping("FishingRods", typeof(FishingRodData), "fishingRods")
            }),
            new ExcelMapping("04_Animals.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Animals", typeof(AnimalData), "animals")
            }),
            new ExcelMapping("05_Combat.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Monsters", typeof(MonsterData), "monsters"),
                new SheetMapping("Bosses", typeof(BossData), "bosses")
            }),
            new ExcelMapping("06_Buildings.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Buildings", typeof(BuildingData), "buildings"),
                new SheetMapping("Commercial", typeof(CommercialBuildingData), "commercialBuildings"),
                new SheetMapping("Tourist", typeof(TouristBuildingData), "touristBuildings")
            }),
            new ExcelMapping("07_NPCs.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Residents", typeof(ResidentData), "residents"),
                new SheetMapping("TouristTypes", typeof(TouristTypeData), "touristTypes")
            }),
            new ExcelMapping("08_Recipes.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Recipes", typeof(RecipeData), "recipes"),
                new SheetMapping("Crafting", typeof(CraftingRecipeData), "craftingRecipes")
            }),
            new ExcelMapping("09_Quests.xlsx", new List<SheetMapping>
            {
                new SheetMapping("MainQuests", typeof(QuestData), "quests"),
                new SheetMapping("SubQuests", typeof(QuestData), "quests")
            }),
            new ExcelMapping("10_Skills.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Skills", typeof(SkillData), "skills")
            }),
            new ExcelMapping("11_Events.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Weekly", typeof(EventData), "events"),
                new SheetMapping("Seasonal", typeof(EventData), "events"),
                new SheetMapping("Random", typeof(EventData), "events")
            }),
            new ExcelMapping("12_Achievements.xlsx", new List<SheetMapping>
            {
                new SheetMapping("Achievements", typeof(AchievementData), "achievements")
            }),
            new ExcelMapping("13_World.xlsx", new List<SheetMapping>
            {
                new SheetMapping("TimeOfDay", typeof(TimeOfDayData), "timeOfDayData"),
                new SheetMapping("Weather", typeof(WeatherData), "weatherData"),
                new SheetMapping("ResourceSpawn", typeof(ResourceSpawnData), "resourceSpawns"),
                new SheetMapping("Areas", typeof(AreaData), "areas")
            }),
            new ExcelMapping("14_Shops.xlsx", new List<SheetMapping>
            {
                new SheetMapping("ShopItems", typeof(ShopItemData), "shopItems")
            })
        };

        [MenuItem("Tools/Sunnyside Island/Convert All Excel Data")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameDataConverterWindow>("GameData Converter");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // 헤더
            GUILayout.Label("선샤인 아일랜드 GameData 변환기", EditorStyles.boldLabel);
            GUILayout.Label("모든 Excel 파일을 ScriptableObject로 변환합니다.", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);
            
            // 경로 설정
            EditorGUILayout.LabelField("설정", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Excel 폴더:", GUILayout.Width(100));
            _excelFolderPath = EditorGUILayout.TextField(_excelFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Excel Folder", _excelFolderPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _excelFolderPath = GetRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("출력 경로:", GUILayout.Width(100));
            _outputPath = EditorGUILayout.TextField(_outputPath);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Output Folder", _outputPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _outputPath = GetRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 진행 상황 바
            if (_isConverting)
            {
                EditorGUILayout.LabelField($"진행 중: {_currentTask}", EditorStyles.boldLabel);
                Rect rect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(rect, _progress, $"{_progress * 100:F0}%");
                EditorGUILayout.Space(10);
            }
            
            // 변환 버튼
            GUI.enabled = !_isConverting;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("모든 Excel 파일 변환", GUILayout.Height(40)))
            {
                ConvertAllExcelFiles();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
            
            EditorGUILayout.Space(5);
            
            // 로그 지우기 버튼
            if (GUILayout.Button("로그 지우기", GUILayout.Height(25)))
            {
                _logs.Clear();
            }
            
            EditorGUILayout.Space(10);
            
            // 로그 출력
            EditorGUILayout.LabelField("변환 로그", EditorStyles.boldLabel);
            
            foreach (var log in _logs)
            {
                Color color = log.Type switch
                {
                    LogType.Success => new Color(0.2f, 0.8f, 0.2f),
                    LogType.Warning => new Color(1f, 0.8f, 0.2f),
                    LogType.Error => new Color(1f, 0.3f, 0.3f),
                    _ => Color.white
                };
                
                GUI.color = color;
                EditorGUILayout.LabelField($"[{log.Time}] {log.Message}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void ConvertAllExcelFiles()
        {
            _logs.Clear();
            _isConverting = true;
            _progress = 0f;
            
            try
            {
                // 출력 폴더 생성
                if (!Directory.Exists(_outputPath))
                {
                    Directory.CreateDirectory(_outputPath);
                    AddLog(LogType.Info, $"출력 폴더 생성: {_outputPath}");
                }
                
                // 기존 GameData.asset 삭제
                var assetPath = Path.Combine(_outputPath, "GameData.asset").Replace("\\", "/");
                var existingAsset = AssetDatabase.LoadAssetAtPath<GameDataContainer>(assetPath);
                if (existingAsset != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    AddLog(LogType.Info, "기존 GameData.asset 삭제");
                }
                
                // 새 GameData 생성
                var gameData = ScriptableObject.CreateInstance<GameDataContainer>();
                int totalFiles = _excelMappings.Count;
                int currentFile = 0;
                
                // 각 Excel 파일 변환
                foreach (var mapping in _excelMappings)
                {
                    currentFile++;
                    _progress = (float)currentFile / totalFiles;
                    _currentTask = $"변환 중: {mapping.FileName}";
                    Repaint();
                    
                    var excelPath = Path.Combine(_excelFolderPath, mapping.FileName);
                    
                    if (!File.Exists(excelPath))
                    {
                        AddLog(LogType.Warning, $"파일 없음: {mapping.FileName}");
                        continue;
                    }
                    
                    try
                    {
                        ConvertExcelFile(excelPath, mapping, gameData);
                        AddLog(LogType.Success, $"완료: {mapping.FileName}");
                    }
                    catch (Exception ex)
                    {
                        AddLog(LogType.Error, $"실패: {mapping.FileName} - {ex.Message}");
                    }
                }
                
                // GameData 저장
                _currentTask = "GameData 저장 중...";
                Repaint();
                
                AssetDatabase.CreateAsset(gameData, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                AddLog(LogType.Success, $"GameData 저장 완료: {assetPath}");
                AddLog(LogType.Success, "모든 변환이 완료되었습니다!");
            }
            catch (Exception ex)
            {
                AddLog(LogType.Error, $"변환 중 오류 발생: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                _isConverting = false;
                _progress = 0f;
                _currentTask = "";
                Repaint();
            }
        }

        private void ConvertExcelFile(string excelPath, ExcelMapping mapping, GameDataContainer gameData)
        {
            var reader = ExcelReaderFactory.Create(excelPath);
            
            foreach (var sheetMapping in mapping.Sheets)
            {
                try
                {
                    // 시트 데이터 읽기
                    var sheetData = reader.ReadSheet(sheetMapping.SheetName).ToList();
                    
                    if (sheetData.Count == 0)
                    {
                        AddLog(LogType.Warning, $"  {mapping.FileName}/{sheetMapping.SheetName}: 빈 시트");
                        continue;
                    }
                    
                    // GameData의 기존 리스트 가져오기 또는 생성
                    var targetList = GetOrCreateList(gameData, sheetMapping.FieldName, sheetMapping.DataType);
                    
                    // 리스트에 데이터 채우기
                    int count = FillList(targetList, sheetMapping.DataType, sheetData);
                    
                    AddLog(LogType.Info, $"  {sheetMapping.SheetName}: {count}개 항목 변환");
                }
                catch (SheetNotFoundException)
                {
                    AddLog(LogType.Warning, $"  시트 없음: {sheetMapping.SheetName}");
                }
                catch (Exception ex)
                {
                    AddLog(LogType.Error, $"  {sheetMapping.SheetName} 변환 실패: {ex.Message}");
                }
            }
        }

        private IList GetOrCreateList(GameDataContainer gameData, string fieldName, Type elementType)
        {
            var gameDataType = typeof(GameDataContainer);
            var field = gameDataType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            
            if (field == null)
            {
                throw new Exception($"Field '{fieldName}' not found in GameData");
            }
            
            var existingList = field.GetValue(gameData) as IList;
            if (existingList == null)
            {
                // field의 실제 타입에서 element type을 추출
                var fieldType = field.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var actualElementType = fieldType.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(actualElementType);
                    existingList = (IList)Activator.CreateInstance(listType);
                    field.SetValue(gameData, existingList);
                }
                else
                {
                    throw new Exception($"Field '{fieldName}' is not a List<T>");
                }
            }
            
            return existingList;
        }

        private int FillList(IList list, Type elementType, List<Dictionary<string, string>> sheetData)
        {
            // list의 실제 element 타입 가져오기 (런타임 어셈블리의 타입)
            var listType = list.GetType();
            var runtimeElementType = listType.GetGenericArguments()[0];
            
            Debug.Log($"[GameDataConverter Debug] FillList:");
            Debug.Log($"  - Editor elementType: {elementType.FullName}");
            Debug.Log($"  - Runtime elementType: {runtimeElementType.FullName}");
            
            // 런타임 타입의 필드 정보 수집
            var fields = runtimeElementType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsDefined(typeof(IgnoreAttribute), false))
                .ToList();
            
            // 컬럼명 추출
            var headerColumns = sheetData.First().Keys.ToList();
            var columnNameToIndex = headerColumns
                .Select((name, index) => new { name, index })
                .ToDictionary(x => x.name.ToLowerInvariant(), x => x.name);
            
            int count = 0;
            
            // 각 행을 객체로 변환
            foreach (var rowData in sheetData)
            {
                // 런타임 타입으로 객체 생성
                var item = Activator.CreateInstance(runtimeElementType);
                
                foreach (var field in fields)
                {
                    var columnName = GetColumnName(field);
                    var actualColumnName = FindColumnName(columnNameToIndex, columnName);
                    
                    if (actualColumnName != null && rowData.TryGetValue(actualColumnName, out var cellValue))
                    {
                        try
                        {
                            var parsedValue = ParseValue(cellValue, field.FieldType, columnName);
                            field.SetValue(item, parsedValue);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to parse value '{cellValue}' for column '{columnName}': {ex.Message}");
                        }
                    }
                }
                
                list.Add(item);
                count++;
            }
            
            return count;
        }

        private void AssignListToGameData(GameDataContainer gameData, string fieldName, IList list, Type elementType)
        {
            var gameDataType = typeof(GameDataContainer);
            var field = gameDataType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            
            if (field == null)
            {
                throw new Exception($"Field '{fieldName}' not found in GameData");
            }
            
            // Quests와 Events는 여러 시트에서 같은 필드에 추가
            if (fieldName == "quests" || fieldName == "events")
            {
                var existingList = field.GetValue(gameData) as IList;
                if (existingList != null)
                {
                    foreach (var item in list)
                    {
                        existingList.Add(item);
                    }
                    return;
                }
            }
            
            field.SetValue(gameData, list);
        }

        private object ParseValue(string value, Type targetType, string columnName)
        {
            if (string.IsNullOrEmpty(value))
            {
                return GetDefaultValue(targetType);
            }
            
            value = value.Trim();
            
            try
            {
                // 기본 타입 처리
                if (targetType == typeof(string))
                    return value;
                if (targetType == typeof(int))
                    return int.Parse(value);
                if (targetType == typeof(float))
                    return float.Parse(value);
                if (targetType == typeof(bool))
                    return value.ToLower() == "true" || value == "1" || value == "yes";
                if (targetType == typeof(double))
                    return double.Parse(value);
                if (targetType == typeof(long))
                    return long.Parse(value);
                if (targetType.IsEnum)
                    return Enum.Parse(targetType, value, true);
                
                // List<string> 처리
                if (targetType == typeof(List<string>))
                {
                    return new List<string>(value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                }
                
                // Dictionary<string, int> 처리
                if (targetType == typeof(Dictionary<string, int>))
                {
                    var dict = new Dictionary<string, int>();
                    var pairs = value.Split(',');
                    foreach (var pair in pairs)
                    {
                        var kv = pair.Trim().Split(':');
                        if (kv.Length == 2)
                        {
                            dict[kv[0].Trim()] = int.Parse(kv[1].Trim());
                        }
                    }
                    return dict;
                }
                
                // Vector2, Vector3 처리
                if (targetType == typeof(Vector2))
                {
                    var parts = value.Split(',').Select(s => float.Parse(s.Trim())).ToArray();
                    return new Vector2(parts[0], parts[1]);
                }
                if (targetType == typeof(Vector3))
                {
                    var parts = value.Split(',').Select(s => float.Parse(s.Trim())).ToArray();
                    return new Vector3(parts[0], parts[1], parts[2]);
                }
                
                // Color 처리
                if (targetType == typeof(Color))
                {
                    var parts = value.Split(',').Select(s => float.Parse(s.Trim()) / 255f).ToArray();
                    return new Color(parts[0], parts[1], parts[2], parts.Length > 3 ? parts[3] : 1f);
                }
                
                throw new NotSupportedException($"Type '{targetType.Name}' is not supported");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse value '{value}' for column '{columnName}' as {targetType.Name}: {ex.Message}");
            }
        }

        private object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        private string GetColumnName(FieldInfo field)
        {
            var columnAttr = field.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr != null)
                return columnAttr.Name;
            return field.Name;
        }

        private string FindColumnName(Dictionary<string, string> columnMap, string targetName)
        {
            var normalizedTarget = targetName.ToLowerInvariant();
            if (columnMap.TryGetValue(normalizedTarget, out var actualName))
                return actualName;
            return null;
        }

        private void AddLog(LogType type, string message)
        {
            _logs.Add(new ConversionLog(type, message));
            if (type == LogType.Error)
            {
                Debug.LogError($"[GameDataConverter] {message}");
            }
            else if (type == LogType.Warning)
            {
                Debug.LogWarning($"[GameDataConverter] {message}");
            }
            else
            {
                Debug.Log($"[GameDataConverter] {message}");
            }
            Repaint();
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

        // 매핑 클래스들
        private class ExcelMapping
        {
            public string FileName { get; }
            public List<SheetMapping> Sheets { get; }
            
            public ExcelMapping(string fileName, List<SheetMapping> sheets)
            {
                FileName = fileName;
                Sheets = sheets;
            }
        }

        private class SheetMapping
        {
            public string SheetName { get; }
            public Type DataType { get; }
            public string FieldName { get; }
            
            public SheetMapping(string sheetName, Type dataType, string fieldName)
            {
                SheetName = sheetName;
                DataType = dataType;
                FieldName = fieldName;
            }
        }
    }
}
