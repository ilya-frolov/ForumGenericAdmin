using System;
using System.Collections.Generic;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Base class for field type plugins with common functionality
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type this plugin handles</typeparam>
    /// <typeparam name="TValue">The value type this plugin operates on</typeparam>
    public abstract class BaseFieldTypePlugin<TAttribute, TValue> : IFieldTypePlugin
        where TAttribute : AdminFieldBaseAttribute
    {
        // Static HttpContextAccessor that is lazily discovered
        private static IHttpContextAccessor _httpContextAccessor;
        
        // Static service provider that is lazily discovered
        private static IServiceProvider _serviceProvider;
        
        // Lock object for thread safety
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the attribute type this plugin handles
        /// </summary>
        public Type AttributeType => typeof(TAttribute);
        
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public virtual string FieldType => FieldTypeHelper.GetFieldTypeFromAttributeType(typeof(TAttribute));

        protected BaseFieldTypePlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets a service of the specified type from the service provider
        /// </summary>
        /// <typeparam name="T">The type of service to get</typeparam>
        /// <returns>The service or default if not found or no service provider is available</returns>
        protected T GetService<T>() where T : class
        {
            return _serviceProvider?.GetService<T>();
        }
        
        #region Non-generic interface implementation

        /// <summary>
        /// Validates a field value before saving - non-generic interface implementation
        /// </summary>
        (bool IsValid, List<string> ErrorMessages) IFieldTypePlugin.Validate(object value, PropertyInfo property)
        {
            // Try to convert the value to TValue
            TValue typedValue = default;
            try
            {
                if (value == null)
                {
                    typedValue = default;
                }
                else if (value is TValue directValue)
                {
                    typedValue = directValue;
                }
                else if (value is IConvertible convertibleValue)
                {
                    // Handle nullable types by getting the underlying type
                    var targetType = typeof(TValue);
                    if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        targetType = Nullable.GetUnderlyingType(targetType);
                    }
                    typedValue = (TValue)convertibleValue.ToType(targetType, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                return (false, new List<string> { $"Value cannot be converted to {typeof(TValue).Name}" });
            }
            
            // Call the strongly-typed implementation
            return Validate(typedValue, property);
        }
        
        /// <summary>
        /// Prepares a database value for use in the model - non-generic interface implementation
        /// </summary>
        object IFieldTypePlugin.PrepareForModel(object dbValue, PropertyInfo property)
        {
            return PrepareForModel(dbValue, property);
        }

        /// <summary>
        /// Prepares a value for saving to the database - non-generic interface implementation
        /// </summary>
        object IFieldTypePlugin.PrepareForDb(object value, PropertyInfo property)
        {
            TValue typedValue = default;

            try
            {
                if (value == null)
                {
                    typedValue = default;
                }
                else if (value is TValue directValue)
                {
                    typedValue = directValue;
                }
                else
                {
                    // Handle Nullable<T>
                    var targetType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
                    var convertedValue = Convert.ChangeType(value, targetType);
                    typedValue = (TValue)convertedValue;
                }
            }
            catch
            {
                return value;
            }

            return PrepareForDb(typedValue, property);
        }

        #endregion

        /// <summary>
        /// Validates a field value before saving
        /// </summary>
        public virtual (bool IsValid, List<string> ErrorMessages) Validate(TValue value, PropertyInfo property)
        {
            var errorMessages = new List<string>();
            
            // Get common attribute to check if field is required
            var commonAttr = property.GetCustomAttribute<AdminFieldCommonAttribute>();
            
            // Check for required fields
            if (commonAttr?.Required == true && EqualityComparer<TValue>.Default.Equals(value, default))
            {
                errorMessages.Add($"Field '{property.Name}' is required");
            }
            
            return (errorMessages.Count == 0, errorMessages);
        }
        
        /// <summary>
        /// Main method that prepares a database value for use in the model.
        /// This method determines whether to use multilanguage logic based on the presence of MultiLanguageAttribute.
        /// </summary>
        public virtual object PrepareForModel(object dbValue, PropertyInfo property)
        {
            // Check if property has MultiLanguageAttribute
            bool isMultiLanguage = property.GetCustomAttribute<MultiLanguageAttribute>() != null;
            
            if (isMultiLanguage && dbValue is string json)
            {
                return PrepareMultiLanguageForModel(json, property);
            }
            
            return PrepareTypedValueForModel(dbValue, property);
        }
        
        /// <summary>
        /// Main method that prepares a value for saving to the database.
        /// This method determines whether to use multilanguage logic based on the presence of MultiLanguageAttribute.
        /// </summary>
        public virtual object PrepareForDb(TValue value, PropertyInfo property)
        {
            // Check if property has MultiLanguageAttribute
            bool isMultiLanguage = property.GetCustomAttribute<MultiLanguageAttribute>() != null;
            
            if (isMultiLanguage)
            {
                return PrepareMultiLanguageForDb(value, property);
            }
            
            return PrepareTypedValueForDb(value, property);
        }
        
        /// <summary>
        /// Prepares a typed value for database storage.
        /// Override this method to implement type-specific logic.
        /// </summary>
        protected virtual object PrepareTypedValueForDb(TValue value, PropertyInfo property)
        {
            return value;
        }
        
        /// <summary>
        /// Prepares a multi-language value for database storage.
        /// </summary>
        private object PrepareMultiLanguageForDb(TValue value, PropertyInfo property)
        {
            // Create a dictionary to store language values
            Dictionary<string, object> processedValues = new Dictionary<string, object>();
            
            // If the value is null, return an empty JSON dictionary
            if (value == null)
            {
                return JsonConvert.SerializeObject(processedValues);
            }
            
            // Handle existing string values that weren't previously multilanguage
            if (value is string stringValue && !stringValue.StartsWith("{") && !stringValue.StartsWith("["))
            {
                // If the value doesn't look like JSON, treat it as a single value for the default language
                processedValues["default"] = PrepareTypedValueForDb(value, property);
                return JsonConvert.SerializeObject(processedValues);
            }
            
            // If already a dictionary, process each language value
            if (value is IDictionary<string, object> languageValues)
            {
                foreach (var entry in languageValues)
                {
                    try
                    {
                        // Try to convert the language value to TValue
                        TValue typedValue;
                        if (entry.Value is TValue directValue)
                        {
                            typedValue = directValue;
                        }
                        else if (entry.Value != null)
                        {
                            typedValue = (TValue)Convert.ChangeType(entry.Value, typeof(TValue));
                        }
                        else
                        {
                            typedValue = default;
                        }
                        
                        // Process the typed value 
                        processedValues[entry.Key] = PrepareTypedValueForDb(typedValue, property);
                    }
                    catch
                    {
                        // If conversion fails, store the original value
                        processedValues[entry.Key] = entry.Value;
                    }
                }
            }
            else
            {
                // Try to parse the value as JSON if it's a string
                try
                {
                    if (value is string jsonString)
                    {
                        var parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
                        if (parsed != null)
                        {
                            // Recursively call this method with the parsed dictionary
                            return PrepareMultiLanguageForDb((TValue)(object)parsed, property);
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
                
                // If not a dictionary and not JSON, treat as a single value for the default language
                processedValues["default"] = PrepareTypedValueForDb(value, property);
            }
            
            // Serialize to JSON for storage
            return JsonConvert.SerializeObject(processedValues);
        }
        
        /// <summary>
        /// Prepares a typed database value for model use.
        /// Override this method to implement type-specific logic.
        /// </summary>
        protected virtual TValue PrepareTypedValueForModel(object dbValue, PropertyInfo property)
        {
            // Get field attribute for any specific handling
            var fieldAttr = property.GetCustomAttribute<TAttribute>();
            
            if (dbValue == null)
            {
                return default;
            }
            
            if (dbValue is TValue typedResult)
            {
                return typedResult;
            }
            
            try
            {
                // Try to convert the result to the expected type
                if (typeof(TValue).IsEnum)
                {
                    return (TValue)Enum.Parse(typeof(TValue), dbValue.ToString());
                }
                else if (typeof(TValue).IsGenericType && typeof(TValue).GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // Handle nullable types
                    var underlyingType = Nullable.GetUnderlyingType(typeof(TValue));
                    var convertedValue = ConvertValueIfNeeded(dbValue, underlyingType);
                    return (TValue)convertedValue;
                }
                else if (IsNumericType(typeof(TValue)) && IsNumericType(dbValue.GetType()))
                {
                    // Handle numeric type conversions
                    var decimalValue = Convert.ToDecimal(dbValue);
                    return (TValue)Convert.ChangeType(decimalValue, typeof(TValue));
                }
                else
                {
                    return (TValue)Convert.ChangeType(dbValue, typeof(TValue));
                }
            }
            catch
            {
                // Handle JSON strings when a field was previously multilanguage but is now single language
                if (dbValue is string jsonStr)
                {
                    try
                    {
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStr);
                        if (dict != null && dict.Count > 0)
                        {
                            // Try to get the default language value, or the first one if default doesn't exist
                            if (dict.TryGetValue("default", out var defaultValue))
                            {
                                return PrepareTypedValueForModel(defaultValue, property);
                            }
                            
                            // If no default language, just take the first one
                            foreach (var entry in dict)
                            {
                                return PrepareTypedValueForModel(entry.Value, property);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore JSON parsing errors
                    }
                }
                
                // Conversion failed, return default
                return default;
            }
        }
        
        /// <summary>
        /// Checks if a type is a numeric type.
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        /// <summary>
        /// Converts a value to the target type if needed.
        /// </summary>
        private static object ConvertValueIfNeeded(object value, Type targetType)
        {
            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (value.GetType() == targetType || targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                // Handle numeric type conversions
                if (IsNumericType(value.GetType()) && IsNumericType(targetType))
                {
                    // Convert through decimal to handle all numeric types
                    var decimalValue = Convert.ToDecimal(value);
                    return Convert.ChangeType(decimalValue, targetType);
                }

                if (targetType.IsEnum)
                    return Enum.Parse(targetType, value.ToString());

                if (targetType == typeof(string))
                    return value.ToString();

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }
        
        /// <summary>
        /// Prepares a multi-language database value for model use.
        /// </summary>
        private Dictionary<string, TValue> PrepareMultiLanguageForModel(string json, PropertyInfo property)
        {
            Dictionary<string, TValue> processedValues = new Dictionary<string, TValue>();
            
            try
            {
                // For multi-language fields, deserialize the JSON to a language dictionary
                var languageValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                
                // If deserialization succeeded and yielded values, process each one
                if (languageValues != null)
                {
                    foreach (var entry in languageValues)
                    {
                        // Process the value with the field's type-specific logic
                        processedValues[entry.Key] = PrepareTypedValueForModel(entry.Value, property);
                    }
                    
                    // Return the processed dictionary as TValue
                    return processedValues;
                }
            }
            catch (Exception ex)
            {
                // If deserialization fails, try to handle it as a non-multilanguage value
            }
            
            // If the field was previously not multilanguage, try to parse it as a single value
            try
            {
                TValue value = PrepareTypedValueForModel(json, property);

                // TODO: It should NOT be 'en', it should be the first of the languages, when we add the relevant support.
                processedValues.Add("en", value);

                return processedValues;
            }
            catch
            {
                // If everything fails, return default
                return default;
            }
        }
    }
} 