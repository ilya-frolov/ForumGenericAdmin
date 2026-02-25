using System;
using System.Collections.Generic;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Non-generic interface for all field type plugins
    /// </summary>
    public interface IFieldTypePlugin
    {
        /// <summary>
        /// Gets the attribute type this plugin handles
        /// </summary>
        Type AttributeType { get; }
        
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        string FieldType { get; }
        
        /// <summary>
        /// Validates a field value before saving
        /// </summary>
        (bool IsValid, List<string> ErrorMessages) Validate(object value, PropertyInfo property);
        
        /// <summary>
        /// Prepares a database value for use in the model
        /// </summary>
        object PrepareForModel(object dbValue, PropertyInfo property);
        
        /// <summary>
        /// Prepares a value for saving to the database
        /// </summary>
        object PrepareForDb(object value, PropertyInfo property);
    }
} 