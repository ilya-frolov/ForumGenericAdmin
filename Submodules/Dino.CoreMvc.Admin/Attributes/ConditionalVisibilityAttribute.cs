using System;

namespace Dino.CoreMvc.Admin.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ShowIfAttribute : Attribute
    {
        public string PropertyName { get; }
        public object[] Values { get; }

        public ShowIfAttribute(string propertyName, params object[] values)
        {
            PropertyName = propertyName;
            Values = values;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class HideIfAttribute : Attribute
    {
        public string PropertyName { get; }
        public object[] Values { get; }

        public HideIfAttribute(string propertyName, params object[] values)
        {
            PropertyName = propertyName;
            Values = values;
        }
    }
} 