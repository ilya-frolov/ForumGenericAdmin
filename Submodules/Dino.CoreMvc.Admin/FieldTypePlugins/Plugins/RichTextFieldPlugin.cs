using System;
using System.Collections.Generic;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.FieldTypePlugins.Plugins.Base;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Plugin for rich text field types
    /// </summary>
    public class RichTextFieldPlugin : TextInputsBasePlugin<AdminFieldRichTextAttribute>
    {
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "RichText";
        
        public RichTextFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
        
        /// <summary>
        /// Prepares a typed value for database storage
        /// </summary>
        protected override object PrepareTypedValueForDb(string value, PropertyInfo property)
        {
            // For rich text, we might want to sanitize the HTML for security
            // This would be a good place to implement that
            if (string.IsNullOrEmpty(value))
                return value;
                
            // Here you could implement HTML sanitization, for example:
            // 1. Remove dangerous scripts
            // 2. Validate and fix HTML structure
            // 3. Convert to specific formats based on editor type
            
            // Simple example (in production you'd use a library):
            string sanitized = value
                .Replace("<script", "&lt;script")
                .Replace("javascript:", "disabled-javascript:");
                
            return sanitized;
        }
    }
} 