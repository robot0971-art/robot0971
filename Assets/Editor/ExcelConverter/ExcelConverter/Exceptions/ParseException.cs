using System;

namespace ExcelConverter.Exceptions
{
    /// <summary>
    /// 값 파싱 중 오류가 발생했을 때 발생하는 예외입니다.
    /// </summary>
    public class ParseException : Exception
    {
        public string Value { get; }
        public Type TargetType { get; }
        public string ColumnName { get; }

        public ParseException(string value, Type targetType, string columnName, Exception innerException = null)
            : base($"Failed to parse value '{value}' to type '{targetType.Name}' in column '{columnName}'.", innerException)
        {
            Value = value;
            TargetType = targetType;
            ColumnName = columnName;
        }
    }
}
