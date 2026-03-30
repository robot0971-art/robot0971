using System;

namespace ExcelConverter.Exceptions
{
    /// <summary>
    /// 지정된 시트를 찾을 수 없을 때 발생하는 예외입니다.
    /// </summary>
    public class SheetNotFoundException : Exception
    {
        public string SheetName { get; }

        public SheetNotFoundException(string sheetName)
            : base($"Sheet '{sheetName}' not found in Excel file.")
        {
            SheetName = sheetName;
        }
    }
}
