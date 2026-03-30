using System;

namespace ExcelConverter.Attributes
{
    /// <summary>
    /// GameData의 List 필드를 특정 시트명과 강제로 매핑합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SheetAttribute : Attribute
    {
        public string Name { get; }

        public SheetAttribute(string name)
        {
            Name = name;
        }
    }
}
