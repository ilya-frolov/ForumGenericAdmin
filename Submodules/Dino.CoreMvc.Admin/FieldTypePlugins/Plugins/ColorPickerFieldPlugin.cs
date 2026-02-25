using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Dino.CoreMvc.Admin.Attributes;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Plugin for color picker field types
    /// </summary>
    public class ColorPickerFieldPlugin : BaseFieldTypePlugin<AdminFieldColorPickerAttribute, string>
    {
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "ColorPicker";

        public ColorPickerFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Validates a color picker field value
        /// </summary>
        public override (bool IsValid, List<string> ErrorMessages) Validate(string value, PropertyInfo property)
        {
            // Get the field attribute
            var fieldAttribute = property.GetCustomAttribute<AdminFieldColorPickerAttribute>();
            
            // First validate using base implementation (handles required fields)
            var baseResult = base.Validate(value, property);
            if (!baseResult.IsValid)
                return baseResult;
            
            // No further validation if value is null or empty
            if (string.IsNullOrEmpty(value))
                return (true, new List<string>());
            
            var errorMessages = new List<string>();
            
            // Basic validation for hex color pattern
            bool isValidFormat = false;
            
            // Validate hex formats
            if (Regex.IsMatch(value, @"^#[0-9A-Fa-f]{6}$"))
            {
                // Regular RGB hex format (#RRGGBB) is always valid
                isValidFormat = true;
            }
            else if (Regex.IsMatch(value, @"^#[0-9A-Fa-f]{8}$"))
            {
                // RGBA hex format (#RRGGBBAA) requires alpha channel to be allowed
                isValidFormat = fieldAttribute.AllowAlpha;
            }
            // Validate functional formats
            else if (Regex.IsMatch(value, @"^rgb\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*\)$"))
            {
                // Regular RGB format (rgb(r,g,b)) is always valid
                isValidFormat = true;
            }
            else if (Regex.IsMatch(value, @"^rgba\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*([01]|0?\.\d+)\s*\)$"))
            {
                // RGBA format (rgba(r,g,b,a)) requires alpha channel to be allowed
                isValidFormat = fieldAttribute.AllowAlpha;
            }
            
            if (!isValidFormat)
            {
                if (fieldAttribute.AllowAlpha)
                {
                    errorMessages.Add($"Field '{property.Name}' has an invalid color format. Expected #RRGGBB, #RRGGBBAA, rgb(r,g,b), or rgba(r,g,b,a)");
                }
                else
                {
                    errorMessages.Add($"Field '{property.Name}' has an invalid color format. Expected #RRGGBB or rgb(r,g,b)");
                }
            }
            
            return (errorMessages.Count == 0, errorMessages);
        }
    }
} 