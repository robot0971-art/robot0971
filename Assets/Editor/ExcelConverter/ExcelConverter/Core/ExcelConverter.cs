using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExcelConverter.Attributes;
using ExcelConverter.Exceptions;
using ExcelConverter.Interfaces;
using ExcelConverter.Parsers;
using UnityEngine;

namespace ExcelConverter.Core
{
    /// <summary>
    /// Excel 파일을 제네릭 GameData로 변환하는 컨버터입니다.
    /// </summary>
    /// <typeparam name="T">ScriptableObject를 상속한 GameData 타입</typeparam>
    public class ExcelConverter<T> where T : ScriptableObject
    {
        private readonly TypeParserRegistry _typeParserRegistry;

        public ExcelConverter()
        {
            _typeParserRegistry = new TypeParserRegistry();
        }

        /// <summary>
        /// 커스텀 타입 파서를 등록합니다.
        /// </summary>
        public void RegisterCustomParser(ICustomTypeParser parser)
        {
            _typeParserRegistry.RegisterParser(parser);
        }

        /// <summary>
        /// Excel 파일을 변환하여 GameData 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="filePath">Excel 파일 경로 (.xlsx 또는 .csv)</param>
        /// <returns>변환된 GameData 인스턴스</returns>
        public T Convert(string filePath)
        {
            var reader = ExcelReaderFactory.Create(filePath);
            var gameData = ScriptableObject.CreateInstance<T>();
            
            var gameDataType = typeof(T);
            var fields = gameDataType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsDefined(typeof(IgnoreAttribute), false));

            foreach (var field in fields)
            {
                // List<T> 필드만 처리
                var fieldType = field.FieldType;
                if (!IsGenericList(fieldType))
                    continue;

                var elementType = fieldType.GetGenericArguments()[0];
                var sheetName = GetSheetName(field);
                
                // 시트 데이터 읽기
                var sheetData = reader.ReadSheet(sheetName).ToList();
                if (sheetData.Count == 0)
                    continue; // 빈 시트는 무시

                // List<T> 생성 및 채우기
                var list = CreateAndFillList(elementType, sheetData);
                field.SetValue(gameData, list);
            }

            return gameData;
        }

        /// <summary>
        /// List<T> 인스턴스를 생성하고 데이터를 채웁니다.
        /// </summary>
        private IList CreateAndFillList(Type elementType, List<Dictionary<string, string>> sheetData)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType);

            // 컬럼명 → 인덱스 매핑 (첫 행에서 추출)
            var headerColumns = sheetData.First().Keys.ToList();
            var columnNameToIndex = headerColumns
                .Select((name, index) => new { name, index })
                .ToDictionary(x => x.name.ToLowerInvariant(), x => x.name);

            // 필드 정보 수집
            var fields = elementType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsDefined(typeof(IgnoreAttribute), false))
                .ToList();

            // 각 행을 객체로 변환
            foreach (var rowData in sheetData)
            {
                var item = Activator.CreateInstance(elementType);
                
                foreach (var field in fields)
                {
                    var columnName = GetColumnName(field);
                    
                    // 컬럼 찾기 (대소문자 무시)
                    var actualColumnName = FindColumnName(columnNameToIndex, columnName);
                    if (actualColumnName == null)
                    {
                        throw new ColumnNotFoundException(columnName, sheetData.ToString());
                    }

                    if (rowData.TryGetValue(actualColumnName, out var cellValue))
                    {
                        var parsedValue = _typeParserRegistry.Parse(cellValue, field.FieldType, columnName);
                        field.SetValue(item, parsedValue);
                    }
                }

                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// 시트 이름을 가져옵니다.
        /// </summary>
        private string GetSheetName(FieldInfo field)
        {
            var sheetAttr = field.GetCustomAttribute<SheetAttribute>();
            if (sheetAttr != null)
                return sheetAttr.Name;
            
            return field.Name;
        }

        /// <summary>
        /// 컬럼 이름을 가져옵니다.
        /// </summary>
        private string GetColumnName(FieldInfo field)
        {
            var columnAttr = field.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr != null)
                return columnAttr.Name;
            
            return field.Name;
        }

        /// <summary>
        /// 대소문자를 무시하고 컬럼명을 찾습니다.
        /// </summary>
        private string FindColumnName(Dictionary<string, string> columnMap, string targetName)
        {
            var normalizedTarget = targetName.ToLowerInvariant();
            
            if (columnMap.TryGetValue(normalizedTarget, out var actualName))
                return actualName;
            
            return null;
        }

        /// <summary>
        /// 타입이 List<T>인지 확인합니다.
        /// </summary>
        private bool IsGenericList(Type type)
        {
            return type.IsGenericType && 
                   (type.GetGenericTypeDefinition() == typeof(List<>) || 
                    type.GetGenericTypeDefinition() == typeof(IList<>));
        }
    }
}
