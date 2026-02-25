using System;
using System.Text.RegularExpressions;
using Dino.CoreMvc.Admin.Attributes;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Helper class for field type operations
    /// </summary>
    public static class FieldTypeHelper
    {
        private static readonly Regex AdminFieldAttributeRegex = new Regex(
            @"^AdminField(.+?)Attribute$",
            RegexOptions.Compiled);
        
        private static readonly Regex AttributeSuffixRegex = new Regex(
            @"^(.+?)Attribute$",
            RegexOptions.Compiled);
            
        /// <summary>
        /// Extracts the field type name from an attribute type
        /// </summary>
        /// <param name="attributeType">The attribute type</param>
        /// <returns>The field type name</returns>
        public static string GetFieldTypeFromAttributeType(Type attributeType)
        {
            if (attributeType == null)
                return null;
                
            // Extract the field type from the attribute name
            // Example: AdminFieldTextAttribute -> Text
            string attributeName = attributeType.Name;
            
            // Try AdminFieldXXXAttribute pattern
            var match = AdminFieldAttributeRegex.Match(attributeName);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            // Try XXXAttribute pattern
            match = AttributeSuffixRegex.Match(attributeName);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            return attributeName;
        }
        
        /// <summary>
        /// Extracts the field type name from an attribute instance
        /// </summary>
        /// <param name="attribute">The attribute instance</param>
        /// <returns>The field type name</returns>
        public static string GetFieldTypeFromAttribute(AdminFieldBaseAttribute attribute)
        {
            if (attribute == null)
                return null;
                
            return GetFieldTypeFromAttributeType(attribute.GetType());
        }
    }
} 