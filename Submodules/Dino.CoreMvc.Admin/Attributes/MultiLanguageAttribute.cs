using System;

namespace Dino.CoreMvc.Admin.Attributes
{
    /// <summary>
    /// Marks a field as supporting multiple languages
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MultiLanguageAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the MultiLanguageAttribute
        /// </summary>
        public MultiLanguageAttribute()
        {
        }
    }
} 