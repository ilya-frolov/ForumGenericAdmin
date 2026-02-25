using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;
using Newtonsoft.Json;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Plugin for multi-select field types
    /// </summary>
    public class MultiSelectFieldPlugin : BaseFieldTypePlugin<AdminFieldMultiSelectAttribute, object>
    {
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "MultiSelect";

        public MultiSelectFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Validates a multi-select field value
        /// </summary>
        public override (bool IsValid, List<string> ErrorMessages) Validate(object value, PropertyInfo property)
        {
            // First validate using base implementation (handles required fields)
            var baseResult = base.Validate(value, property);
            if (!baseResult.IsValid)
                return baseResult;

            // No further validation if value is null
            if (value == null)
                return (true, new List<string>());

            // Multi-select specific validation could be added here if needed
            // For example, checking that the value is a collection/array
            if (!(value is IEnumerable) && !(value is Array) && !(value is IList))
            {
                return (false, new List<string> { $"Field '{property.Name}' must contain multiple values for a multi-select field" });
            }

            return (true, new List<string>());
        }

        /// <summary>
        /// Prepares a typed value for database storage
        /// </summary>
        protected override object PrepareTypedValueForDb(object value, PropertyInfo property)
        {
            if (value == null)
                return null;

            var attribute = property.GetCustomAttribute<AdminFieldMultiSelectAttribute>();
            if (attribute == null)
                return null;

            // If value is a single item (not a collection), wrap it in a list
            if (!(value is IEnumerable valueCollection) && !(value is Array) && !(value is IList))
            {
                value = new List<object> { value };
            }

            // Handle JSON storage
            if (attribute.StoreAsJson)
            {
                // Convert to JSON if it's a collection
                if (value is IEnumerable enumerableJson && !(value is string))
                {
                    return JsonConvert.SerializeObject(value);
                }
                return value;
            }
            
            // For EF relationships, we just return the collection of IDs
            // The actual relationship management will be handled by the mapping logic
            if (value is IEnumerable enumerable && !(value is string))
            {
                return enumerable.Cast<object>().ToList();
            }

            return value;
        }

        /// <summary>
        /// Prepares a database value for model use.
        /// Note: For EF relationships (StoreAsJson = false), this is handled by 
        /// ModelMappingExtensions.HandleMultiSelectRelationshipFromDb() instead.
        /// </summary>
        protected override object PrepareTypedValueForModel(object dbValue, PropertyInfo property)
        {
            var attribute = property.GetCustomAttribute<AdminFieldMultiSelectAttribute>();
            if (attribute == null)
                return new List<object>();

            if (dbValue == null)
                return new List<object>();

            // Handle JSON storage
            if (attribute.StoreAsJson)
            {
                // If it's a JSON string, deserialize it
                if (dbValue is string json && !string.IsNullOrWhiteSpace(json) && 
                    (json.StartsWith("[") || json.StartsWith("{")))
                {
                    try
                    {
                        var deserialized = JsonConvert.DeserializeObject<List<object>>(json);
                        if (deserialized != null)
                            return deserialized;
                    }
                    catch
                    {
                        // Ignore JSON parsing errors
                    }
                }
            }
            
            // For EF relationships, this method won't be called - 
            // HandleMultiSelectRelationshipFromDb in ModelMappingExtensions handles it.
            // But if somehow we get here, just return the value as-is.
            if (dbValue is IEnumerable<object> collection)
            {
                return collection.ToList();
            }

            // If it's a single value or we couldn't process it properly, wrap it in a list
            if (!(dbValue is IEnumerable<object>) && !(dbValue is Array) && !(dbValue is List<object>))
            {
                return new List<object> { dbValue };
            }

            return dbValue;
        }
    }
}