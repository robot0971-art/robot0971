using System;
using System.IO;
using System.Linq;
using ExcelConverter.Interfaces;

namespace ExcelConverter.Core
{
    /// <summary>
    /// 파일 경로로부터 적절한 IExcelReader를 생성하는 팩토리입니다.
    /// </summary>
    public static class ExcelReaderFactory
    {
        /// <summary>
        /// 파일 확장자에 따라 적절한 리더를 생성합니다.
        /// </summary>
        /// <param name="filePath">xlsx 또는 csv 파일 경로</param>
        /// <returns>IExcelReader 인스턴스</returns>
        public static IExcelReader Create(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".xlsx":
                case ".xls":
                    return new XlsxReader(filePath);
                case ".csv":
                    return new CsvReader(filePath);
                default:
                    throw new NotSupportedException($"File extension '{extension}' is not supported. Supported extensions: .xlsx (requires ExcelDataReader), .csv");
            }
        }
    }
}
