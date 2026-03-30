using System;

namespace ExcelConverter.Attributes
{
    /// <summary>
    /// 필드를 특정 엑셀 컬럼명과 강제로 매핑합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; }

        public ColumnAttribute(string name)
        {
            Name = name;
        }
    }
}
