using System;

namespace ExcelConverter.Attributes
{
    /// <summary>
    /// 변환에서 해당 필드를 제외합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class IgnoreAttribute : Attribute
    {
    }
}
