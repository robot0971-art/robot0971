using System;

namespace ExcelConverter.Exceptions
{
    /// <summary>
    /// 지정된 컬럼을 찾을 수 없을 때 발생하는 예외입니다.
    /// </summary>
    public class ColumnNotFoundException : Exception
    {
        public string ColumnName { get; }
        public string SheetName { get; }

        public ColumnNotFoundException(string columnName, string sheetName)
            : base($"Column '{columnName}' not found in sheet '{sheetName}'.")
        {
            ColumnName = columnName;
            SheetName = sheetName;
        }
    }
}
