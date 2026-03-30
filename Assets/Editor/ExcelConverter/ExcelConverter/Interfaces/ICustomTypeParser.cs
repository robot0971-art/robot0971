using System;

namespace ExcelConverter.Interfaces
{
    /// <summary>
    /// 커스텀 타입 파서를 구현하기 위한 인터페이스입니다.
    /// </summary>
    public interface ICustomTypeParser
    {
        /// <summary>
        /// 해당 타입을 파싱할 수 있는지 확인합니다.
        /// </summary>
        bool CanParse(Type targetType);

        /// <summary>
        /// 문자열 값을 대상 타입으로 파싱합니다.
        /// </summary>
        object Parse(string value, Type targetType);
    }
}
