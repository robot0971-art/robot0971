using System;

namespace DI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class InjectAttribute : Attribute
    {
        public string Key { get; }
        public bool Optional { get; set; }
        
        public InjectAttribute(string key = "")
        {
            Key = key;
            Optional = false;
        }
    }
}
