using System;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Plugin for checkbox field types
    /// </summary>
    public class CheckboxFieldPlugin : BaseFieldTypePlugin<AdminFieldCheckboxAttribute, bool>
    {
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "Checkbox";

        public CheckboxFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Prepares a typed value for database storage
        /// </summary>
        protected override object PrepareTypedValueForDb(bool value, PropertyInfo property)
        {
            // Booleans are straightforward, no special handling needed
            return value;
        }

        /// <summary>
        /// Prepares a database value for model use
        /// </summary>
        protected override bool PrepareTypedValueForModel(object dbValue, PropertyInfo property)
        {
            if (dbValue == null)
                return false;

            // If it's already a boolean, return it
            if (dbValue is bool boolValue)
                return boolValue;

            // Handle string conversions
            if (dbValue is string strValue)
            {
                if (bool.TryParse(strValue, out bool result))
                    return result;
                
                // Also handle "1"/"0", "yes"/"no", etc.
                switch (strValue.ToLowerInvariant())
                {
                    case "1":
                    case "yes":
                    case "true":
                    case "on":
                        return true;
                    default:
                        return false;
                }
            }

            // Handle numeric values
            if (dbValue is int intValue)
                return intValue != 0;

            // Default to false for any other value
            return false;
        }
    }
} 