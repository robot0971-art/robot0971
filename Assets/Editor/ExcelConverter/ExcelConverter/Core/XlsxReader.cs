using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ExcelDataReader;
using ExcelConverter.Interfaces;

namespace ExcelConverter.Core
{
    /// <summary>
    /// xlsx 파일을 읽기 위한 ExcelDataReader 구현체입니다.
    /// </summary>
    public class XlsxReader : IExcelReader
    {
        private DataSet _dataSet;
        private bool _disposed;

        public XlsxReader(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel file not found: {filePath}");

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = global::ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
            
            // AsDataSet 없이 직접 DataSet 구성
            _dataSet = new DataSet();
            
            do
            {
                var table = new DataTable(reader.Name);
                
                // 컬럼 생성
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    table.Columns.Add($"Column{i}", typeof(string));
                }
                
                // 데이터 읽기
                while (reader.Read())
                {
                    var row = table.NewRow();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
                    }
                    table.Rows.Add(row);
                }
                
                // 첫 행을 헤더로 설정
                if (table.Rows.Count > 0)
                {
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        var headerValue = table.Rows[0][i]?.ToString();
                        if (!string.IsNullOrEmpty(headerValue))
                        {
                            table.Columns[i].ColumnName = headerValue;
                        }
                    }
                }
                
                _dataSet.Tables.Add(table);
            } while (reader.NextResult());
        }

        public IEnumerable<string> GetSheetNames()
        {
            EnsureNotDisposed();
            return _dataSet.Tables.Cast<DataTable>().Select(t => t.TableName);
        }

        public IEnumerable<Dictionary<string, string>> ReadSheet(string sheetName = null)
        {
            EnsureNotDisposed();

            DataTable table;
            
            if (string.IsNullOrEmpty(sheetName))
            {
                table = _dataSet.Tables[0];
            }
            else
            {
                table = _dataSet.Tables[sheetName];
                if (table == null)
                    throw new Exceptions.SheetNotFoundException(sheetName);
            }

            if (table.Rows.Count <= 1) // 헤더만 있거나 비어있음
                yield break;

            // 데이터 읽기 (2행부터, 1행은 헤더)
            for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                var dict = new Dictionary<string, string>();
                bool hasData = false;

                for (int colIndex = 0; colIndex < table.Columns.Count; colIndex++)
                {
                    var columnName = table.Columns[colIndex].ColumnName;
                    var value = row[colIndex]?.ToString() ?? string.Empty;
                    dict[columnName] = value;
                    if (!string.IsNullOrWhiteSpace(value))
                        hasData = true;
                }

                if (hasData)
                    yield return dict;
            }
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _dataSet?.Dispose();
                _dataSet = null;
                _disposed = true;
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(XlsxReader));
        }
    }
}
