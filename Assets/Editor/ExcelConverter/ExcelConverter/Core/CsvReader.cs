using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExcelConverter.Interfaces;

namespace ExcelConverter.Core
{
    /// <summary>
    /// CSV 파일을 읽기 위한 구현체입니다.
    /// </summary>
    public class CsvReader : IExcelReader
    {
        private readonly string _filePath;
        private readonly char _delimiter;
        private readonly Encoding _encoding;
        private bool _disposed;

        /// <summary>
        /// CSV 리더를 생성합니다.
        /// </summary>
        /// <param name="filePath">CSV 파일 경로</param>
        /// <param name="delimiter">구분자 (기본: 쉼표)</param>
        /// <param name="encoding">인코딩 (기본: UTF-8)</param>
        public CsvReader(string filePath, char delimiter = ',', Encoding encoding = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"CSV file not found: {filePath}");

            _filePath = filePath;
            _delimiter = delimiter;
            _encoding = encoding ?? Encoding.UTF8;
        }

        public IEnumerable<string> GetSheetNames()
        {
            // CSV는 시트가 없으므로 파일명(확장자 제외)을 반환
            yield return Path.GetFileNameWithoutExtension(_filePath);
        }

        public IEnumerable<Dictionary<string, string>> ReadSheet(string sheetName = null)
        {
            EnsureNotDisposed();

            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream, _encoding);

            // 헤더 읽기
            var headerLine = reader.ReadLine();
            if (string.IsNullOrEmpty(headerLine))
                yield break;

            var headers = ParseCsvLine(headerLine);

            // 데이터 읽기
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var values = ParseCsvLine(line);
                var dict = new Dictionary<string, string>();
                bool hasData = false;

                for (int i = 0; i < headers.Count && i < values.Count; i++)
                {
                    var value = values[i].Trim();
                    dict[headers[i]] = value;
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
            _disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CsvReader));
        }

        /// <summary>
        /// CSV 라인을 파싱합니다. (큰따옴표 처리 포함)
        /// </summary>
        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // 이스케이프된 큰따옴표
                        current.Append('"');
                        i++; // 다음 문자 스킵
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == _delimiter && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString().Trim());
            return result;
        }
    }
}
