using System;
using System.Collections.Generic;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Plugin for select field types
    /// </summary>
    public class SelectFieldPlugin : BaseFieldTypePlugin<AdminFieldSelectAttribute, object>
    {
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "Select";

        public SelectFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Validates a select field value
        /// </summary>
        public override (bool IsValid, List<string> ErrorMessages) Validate(object value, PropertyInfo property)
        {
            var fieldAttribute = property.GetCustomAttribute<AdminFieldSelectAttribute>();
            
            // First validate using base implementation (handles required fields)
            var baseResult = base.Validate(value, property);
            if (!baseResult.IsValid)
                return baseResult;
            
            // No further validation if value is null
            if (value == null)
                return (true, new List<string>());
                
            var errorMessages = new List<string>();
            
            return (errorMessages.Count == 0, errorMessages);
        }

        /// <summary>
        /// Prepares a typed value for database storage
        /// </summary>
        protected override object PrepareTypedValueForDb(object value, PropertyInfo property)
        {
            if (value == null)
                return null;
                
            var fieldAttribute = property.GetCustomAttribute<AdminFieldSelectAttribute>();
            
            // No special handling needed
            return value;
        }

        /// <summary>
        /// Prepares a database value for model use
        /// </summary>
        protected override object PrepareTypedValueForModel(object dbValue, PropertyInfo property)
        {
            if (dbValue == null)
                return null;
                
            var fieldAttribute = property.GetCustomAttribute<AdminFieldSelectAttribute>();
            
            // Enum conversion
            if (fieldAttribute.SourceType == SelectSourceType.Enum)
            {
                var enumType = fieldAttribute.OptionsSource as Type;
                if (enumType != null && enumType.IsEnum)
                {
                    // Try to convert to the enum value
                    try
                    {
                        if (dbValue is string strValue)
                        {
                            return Enum.Parse(enumType, strValue);
                        }
                        else
                        {
                            // Get the underlying type of the enum. Might be short, int, etc.
                            var underlyingType = Enum.GetUnderlyingType(enumType);
                            
                            // Convert the value to the correct underlying type
                            var convertedValue = Convert.ChangeType(dbValue, underlyingType);
                            
                            return Enum.ToObject(enumType, convertedValue);
                        }
                    }
                    catch
                    {
                        // If conversion fails, return the original value
                        return dbValue;
                    }
                }
            }
            
            // For regular selects, return as is
            return base.PrepareTypedValueForModel(dbValue, property);
        }
    }
} 