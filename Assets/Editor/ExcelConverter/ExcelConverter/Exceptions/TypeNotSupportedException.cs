using System;

namespace ExcelConverter.Exceptions
{
    /// <summary>
    /// 지원하지 않는 타입을 변환하려 할 때 발생하는 예외입니다.
    /// </summary>
    public class TypeNotSupportedException : Exception
    {
        public Type TargetType { get; }

        public TypeNotSupportedException(Type targetType)
            : base($"Type '{targetType.Name}' is not supported. Please register a custom parser or use supported types.")
        {
            TargetType = targetType;
        }
    }
}
