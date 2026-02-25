using System;
using System.Collections.Generic;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Plugin for number field types
    /// </summary>
    public class NumberFieldPlugin : BaseFieldTypePlugin<AdminFieldNumberAttribute, double?>
    {
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "Number";

        public NumberFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Validates a number field value
        /// </summary>
        public override (bool IsValid, List<string> ErrorMessages) Validate(double? value, PropertyInfo property)
        {
            // Get the field attribute
            var fieldAttribute = property.GetCustomAttribute<AdminFieldNumberAttribute>();
            
            // First validate using base implementation (handles required fields)
            var baseResult = base.Validate(value, property);
            if (!baseResult.IsValid)
                return baseResult;
            
            // No further validation if value is null
            if (value == null)
                return (true, new List<string>());
            
            var errorMessages = new List<string>();
            
            // Check minimum value if specified (not using sentinel value)
            if (fieldAttribute.Min != double.MinValue && value < fieldAttribute.Min)
            {
                errorMessages.Add($"Field '{property.Name}' must be at least {fieldAttribute.Min}");
            }
            
            // Check maximum value if specified (not using sentinel value)
            if (fieldAttribute.Max != double.MaxValue && value > fieldAttribute.Max)
            {
                errorMessages.Add($"Field '{property.Name}' must not exceed {fieldAttribute.Max}");
            }
            
            // Check decimal places if it's a decimal value and validation is enabled (not -1)
            if (fieldAttribute.IsDecimal && value.HasValue && fieldAttribute.DecimalPlaces != -1)
            {
                var stringValue = value.ToString();
                var decimalPointIndex = stringValue.IndexOf('.');

                if (decimalPointIndex >= 0)
                {
                    var decimalPlaces = stringValue.Length - decimalPointIndex - 1;
                    if (decimalPlaces > fieldAttribute.DecimalPlaces)
                    {
                        errorMessages.Add(
                            $"Field '{property.Name}' must have at most {fieldAttribute.DecimalPlaces} decimal places");
                    }
                }
            }
            
            return (errorMessages.Count == 0, errorMessages);
        }
        
        /// <summary>
        /// Prepares a typed value for database storage with number formatting considerations
        /// </summary>
        protected override object PrepareTypedValueForDb(double? value, PropertyInfo property)
        {
            if (value == null)
                return null;
            
            var fieldAttribute = property.GetCustomAttribute<AdminFieldNumberAttribute>();
            
            // If this is an integer field, we should return an integer value
            if (!fieldAttribute.IsDecimal)
            {
                return Convert.ToInt32(value);
            }
            
            // For decimal values, round to the specified decimal places if validation is enabled (not -1)
            if (fieldAttribute.IsDecimal && fieldAttribute.DecimalPlaces != -1)
            {
                return Math.Round(value.Value, fieldAttribute.DecimalPlaces);
            }
            
            return value;
        }
    }
} 