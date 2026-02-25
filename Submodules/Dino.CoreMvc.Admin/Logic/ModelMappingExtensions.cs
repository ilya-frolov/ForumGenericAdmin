using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Dino.Core.AdminBL.Helpers;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.FieldTypePlugins;
using Dino.CoreMvc.Admin.Models;
using Dino.CoreMvc.Admin.Models.Admin;
using Newtonsoft.Json;

namespace Dino.CoreMvc.Admin.Logic
{
    /// <summary>
    /// Context information for model mapping operations.
    /// </summary>
    public class ModelMappingContext
    {
        /// <summary>
        /// Gets or sets the field type plugin registry.
        /// </summary>
        public FieldTypePluginRegistry PluginRegistry { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the current user performing the operation.
        /// </summary>
        public object CurrentUserId { get; set; }

        /// <summary>
        /// Tracks the depth of nested JSON objects being processed.
        /// A value > 0 indicates we're in a JSON serialization context.
        /// </summary>
        public int JsonProcessingDepth { get; set; } = 0;

        /// <summary>
        /// Returns true if we're currently processing a complex object that will be stored as JSON.
        /// </summary>
        public bool InInnerProcessingOfJson => JsonProcessingDepth > 0;

        /// <summary>
        /// Set of property names that were included in the original request JSON.
        /// Used to preserve existing database values for properties not in the request.
        /// </summary>
        public HashSet<string> IncludedProperties { get; set; }
        
        /// <summary>
        /// Gets or sets the DbContext for database operations.
        /// Used for multi-select relationships to find existing entities.
        /// </summary>
        public object DbContext { get; set; }
    }

    /// <summary>
    /// Static methods to work with ExpandoObject, providing property mapping functionality.
    /// </summary>
    public static class ExpandoObjectExtensions
    {
        /// <summary>
        /// Creates a new ExpandoObject and initializes it from a model object
        /// </summary>
        /// <param name="modelObject">The model object to copy properties from</param>
        /// <param name="existingJsonData">Optional existing JSON data</param>
        /// <returns>An initialized ExpandoObject</returns>
        public static ExpandoObject CreateFrom(object modelObject, string existingJsonData = null)
        {
            var result = new ExpandoObject();
            var targetDict = (IDictionary<string, object>)result;
            
            // Initialize from existing JSON data first (if available)
            if (!string.IsNullOrEmpty(existingJsonData))
            {
                try
                {
                    // Deserialize to dynamic
                    var existingData = JsonConvert.DeserializeObject<ExpandoObject>(existingJsonData);
                    if (existingData != null)
                    {
                        // Copy all existing properties
                        var sourceDict = (IDictionary<string, object>)existingData;
                        
                        foreach (var kvp in sourceDict)
                        {
                            targetDict[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch
                {
                    // Ignore deserialization errors and continue with empty object
                }
            }

            // Then add model properties only if they don't already exist in the target
            if (modelObject != null)
            {
                var properties = modelObject.GetType().GetProperties();
                
                foreach (var prop in properties)
                {
                    // Skip non-readable properties
                    if (!prop.CanRead)
                        continue;
                        
                    // Only add properties that don't already exist
                    if (!targetDict.ContainsKey(prop.Name))
                    {
                        try
                        {
                            var value = prop.GetValue(modelObject);
                            targetDict[prop.Name] = value;
                        }
                        catch
                        {
                            // Ignore any property access errors
                        }
                    }
                }
            }
            
            return result;
        }
    }

    /// <summary>
    /// Provides extension methods for mapping between admin models and database entities.
    /// </summary>
    public static class ModelMappingExtensions
    {
        /// <summary>
        /// Maps from a database entity to an admin model.
        /// </summary>
        /// <typeparam name="TModel">The admin model type</typeparam>
        /// <typeparam name="TEFEntity">The database entity type</typeparam>
        /// <param name="dbEntity">The database entity to map from</param>
        /// <param name="context">The mapping context with registry and user information</param>
        /// <returns>A tuple containing the model and validation errors</returns>
        public static (TModel Model, Dictionary<string, List<DinoAdminConvertError>> ValidationErrors) ToAdminModel<TModel, TEFEntity>(this TEFEntity dbEntity, ModelMappingContext context)
            where TModel : BaseAdminModel, new()
            where TEFEntity : class
        {
            if (dbEntity == null)
                return (null, new Dictionary<string, List<DinoAdminConvertError>>());

            // Create a new admin model instance
            var model = new TModel();
            
            // Create validation errors collection
            var validationErrors = new Dictionary<string, List<DinoAdminConvertError>>();
            
            // Map properties from database entity to model
            MapFromDbEntityToModel(dbEntity, model, context, validationErrors);
            
            return (model, validationErrors);
        }

        /// <summary>
        /// Maps from a database entity to an admin model using runtime types.
        /// </summary>
        /// <param name="dbEntity">The database entity to map from</param>
        /// <param name="modelType">The type of the admin model</param>
        /// <param name="entityType">The type of the database entity</param>
        /// <param name="context">The mapping context with registry and user information</param>
        /// <returns>A tuple containing the model and validation errors</returns>
        public static (object Model, Dictionary<string, List<DinoAdminConvertError>> ValidationErrors) ToAdminModelFromTypes(
            object dbEntity,
            Type modelType,
            Type entityType,
            ModelMappingContext context)
        {
            if (dbEntity == null)
                return (null, new Dictionary<string, List<DinoAdminConvertError>>());

            if (!typeof(BaseAdminModel).IsAssignableFrom(modelType))
                return (null, new Dictionary<string, List<DinoAdminConvertError>> { 
                    { "TypeError", new List<DinoAdminConvertError> { 
                        new DinoAdminConvertError { 
                            Message = "Model type must inherit from BaseAdminModel" 
                        } 
                    } } 
                });

            if (!entityType.IsClass)
                return (null, new Dictionary<string, List<DinoAdminConvertError>> { 
                    { "TypeError", new List<DinoAdminConvertError> { 
                        new DinoAdminConvertError { 
                            Message = "Entity type must be a class" 
                        } 
                    } } 
                });

            try
            {
                // Get the generic ToAdminModelFromTypes method
                var toAdminModelMethod = typeof(ModelMappingExtensions)
                    .GetMethod(nameof(ToAdminModel))
                    .MakeGenericMethod(modelType, entityType);

                // Call the generic method
                var result = (dynamic)toAdminModelMethod.Invoke(null, new[] { dbEntity, context });
                return (result.Item1, result.Item2);
            }
            catch (Exception ex)
            {
                return (null, new Dictionary<string, List<DinoAdminConvertError>> { 
                    { "MappingError", new List<DinoAdminConvertError> { 
                        new DinoAdminConvertError { 
                            Message = $"Error mapping entity to model: {ex.Message}" 
                        } 
                    } } 
                });
            }
        }

        /// <summary>
        /// Maps from an admin model to a database entity.
        /// </summary>
        /// <typeparam name="TModel">The admin model type</typeparam>
        /// <typeparam name="TEFEntity">The database entity type</typeparam>
        /// <param name="model">The admin model to map from</param>
        /// <param name="context">The mapping context with registry and user information</param>
        /// <param name="dbEntity">Optional existing database entity to update</param>
        /// <returns>A new or updated database entity with values from the admin model</returns>
        public static (TEFEntity Entity, ModelConvertResult ConvertResult) ToDbEntity<TModel, TEFEntity>(this TModel model, ModelMappingContext context, TEFEntity dbEntity = null)
            where TModel : BaseAdminModel, new()
            where TEFEntity : class, new()
        {
            if (model == null)
                return (null, new ModelConvertResult { Success = false, ErrorMessage = "Model is null" });

            // Create a new entity instance if one was not provided
            dbEntity ??= new TEFEntity();
            
            // Map properties from model to database entity
            var validationErrors = new Dictionary<string, List<DinoAdminConvertError>>();
            MapFromModelToDbEntity(model, dbEntity, context, validationErrors);
            
            // Create and return save result
            var saveResult = new ModelConvertResult
            {
                Success = !validationErrors.Any(),
                ValidationErrors = validationErrors,
                ErrorMessage = validationErrors.Any() ? "Validation errors occurred" : null,
                EntityId = GetEntityId(dbEntity)
            };
            
            return (dbEntity, saveResult);
        }

        /// <summary>
        /// Maps from an admin model to a database entity using runtime types.
        /// </summary>
        /// <param name="model">The admin model to map from</param>
        /// <param name="modelType">The type of the admin model</param>
        /// <param name="entityType">The type of the database entity</param>
        /// <param name="context">The mapping context with registry and user information</param>
        /// <param name="dbEntity">Optional existing database entity to update</param>
        /// <returns>A tuple containing the entity and conversion result</returns>
        public static (object Entity, ModelConvertResult ConvertResult) ToDbEntityFromTypes(
            object model, 
            Type modelType, 
            Type entityType, 
            ModelMappingContext context, 
            object dbEntity = null)
        {
            if (model == null)
                return (null, new ModelConvertResult { Success = false, ErrorMessage = "Model is null" });

            if (!typeof(BaseAdminModel).IsAssignableFrom(modelType))
                return (null, new ModelConvertResult { Success = false, ErrorMessage = "Model type must inherit from BaseAdminModel" });

            if (!entityType.IsClass)
                return (null, new ModelConvertResult { Success = false, ErrorMessage = "Entity type must be a class" });

            try
            {
                // Get the generic ToDbEntityFromTypes method
                var toDbEntityMethod = typeof(ModelMappingExtensions)
                    .GetMethod(nameof(ToDbEntity))
                    .MakeGenericMethod(modelType, entityType);

                // Call the generic method
                var result = (dynamic)toDbEntityMethod.Invoke(null, new[] { model, context, dbEntity });
                return (result.Item1, result.Item2);
            }
            catch (Exception ex)
            {
                return (null, new ModelConvertResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Error mapping model to entity: {ex.Message}" 
                });
            }
        }

        
        /// <summary>
        /// Maps properties from a database entity to an admin model.
        /// </summary>
        private static void MapFromDbEntityToModel<TModel, TEFEntity>(TEFEntity dbEntity, TModel model, ModelMappingContext context, 
            Dictionary<string, List<DinoAdminConvertError>> validationErrors, string propertyPath = "")
            where TModel : BaseAdminModel, new()
            where TEFEntity : class
        {
            // Call pre-mapping and check if we should continue
            if (model is BaseAdminModel baseAdminModel)
            {
                try
                {
                    if (!baseAdminModel.CustomPreMapFromDbModel(dbEntity, model, context))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AddValidationError(validationErrors, "CustomMapping", 
                        $"Error in pre-mapping: {ex.Message}", 
                        "CustomMappingError");
                    return;
                }
            }

            // Get the properties of the model and entity
            var modelProperties = typeof(TModel).GetProperties();
            var entityProperties = typeof(TEFEntity).GetProperties()
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            // Process each model property
            foreach (var modelProperty in modelProperties)
            {
                try
                {
                    // Skip properties marked with SkipMapping attribute (SkipFromDb = true)
                    var skipAttribute = modelProperty.GetAttribute<SkipMappingAttribute>();
                    if (skipAttribute?.SkipFromDb == true)
                        continue;

                    // Try to find the corresponding entity property
                    if (!entityProperties.TryGetValue(modelProperty.Name, out var entityProperty))
                    {
                        // Property not found in the entity
                        string propPath = GetNestedPropertyPath(propertyPath, modelProperty.Name);
                            
                        AddValidationError(validationErrors, propPath, 
                            $"Property '{modelProperty.Name}' not found in entity '{typeof(TEFEntity).Name}'", 
                            "MissingPropertyError");
                        continue;
                    }

                    // Get the entity property value
                    var dbValue = entityProperty.GetValue(dbEntity);

                    // Handle complex types
                    var complexAttribute = modelProperty.GetAttribute<BaseComplexAttribute>();
                    if (complexAttribute != null)
                    {
                        string nestedPropertyPath = GetNestedPropertyPath(propertyPath, modelProperty.Name);
                            
                        HandleComplexTypeFromDb(modelProperty, dbValue, model, context, validationErrors, nestedPropertyPath);
                        continue;
                    }

                    // Get the field type attribute
                    var fieldTypeAttribute = GetFieldTypeAttribute(modelProperty);
                    if (fieldTypeAttribute != null)
                    {
                        // Handle multi-select relationships specially (symmetric with Model → DB direction)
                        if (fieldTypeAttribute is AdminFieldMultiSelectAttribute multiSelectAttr && !multiSelectAttr.StoreAsJson)
                        {
                            string propPath = GetNestedPropertyPath(propertyPath, modelProperty.Name);
                            HandleMultiSelectRelationshipFromDb(multiSelectAttr, dbValue, modelProperty, model, validationErrors, propPath);
                            continue;
                        }
                        
                        // Use field type plugin to prepare the value for the model
                        var plugin = context.PluginRegistry.GetPlugin(fieldTypeAttribute);
                        if (plugin != null)
                        {
                            try
                            {
                                // Use dynamic to call PrepareForModel which is not directly in the interface
                                var preparedValue = plugin.PrepareForModel((dynamic)dbValue, modelProperty);
                                
                                // Convert the prepared value to the correct type before setting
                                var convertedValue = ConvertValueIfNeeded(preparedValue, modelProperty?.PropertyType);
                                SetPropertyValue(model, modelProperty, convertedValue, modelProperty.Name);
                            }
                            catch (Exception ex)
                            {
                                string propPath = GetNestedPropertyPath(propertyPath, modelProperty.Name);
                                
                                AddValidationError(validationErrors, propPath, 
                                    $"Error preparing field: {ex.Message}", 
                                    "FieldTypePluginError");
                            }
                            continue;
                        }
                    }

                    // Handle simple types directly
                    try
                    {
                        SetPropertyValue(model, modelProperty, ConvertValueIfNeeded(dbValue, modelProperty?.PropertyType), modelProperty.Name);
                    }
                    catch (Exception ex)
                    {
                        string propPath = GetNestedPropertyPath(propertyPath, modelProperty.Name);
                            
                        AddValidationError(validationErrors, propPath, 
                            $"Error converting value: {ex.Message}", 
                            "ValueConversionError");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error and add to validation errors
                    string propPath = GetNestedPropertyPath(propertyPath, modelProperty.Name);
                        
                    AddValidationError(validationErrors, propPath, 
                        $"Error mapping property: {ex.Message}", 
                        "PropertyMappingError");
                    Console.WriteLine($"Error mapping {modelProperty.Name}: {ex.Message}");
                }
            }

            // Call post-mapping after the main mapping is done
            if (model is BaseAdminModel baseAdminModelPost)
            {
                try
                {
                    baseAdminModelPost.CustomPostMapFromDbModel(dbEntity, model, context);
                }
                catch (Exception ex)
                {
                    AddValidationError(validationErrors, "CustomMapping", 
                        $"Error in post-mapping: {ex.Message}", 
                        "CustomMappingError");
                }
            }
        }

        /// <summary>
        /// Maps properties from an admin model to a database entity.
        /// </summary>
        private static void MapFromModelToDbEntity<TModel, TEFEntity>(TModel model, TEFEntity dbEntity, ModelMappingContext context, 
            Dictionary<string, List<DinoAdminConvertError>> validationErrors, string propertyPath = "")
            where TModel : BaseAdminModel, new()
            where TEFEntity : class
        {
            // Call pre-mapping and check if we should continue
            if (model is BaseAdminModel baseAdminModel)
            {
                try
                {
                    if (!baseAdminModel.CustomPreMapToDbModel(model, dbEntity, context))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AddValidationError(validationErrors, "CustomMapping", 
                        $"Error in pre-mapping: {ex.Message}", 
                        "CustomMappingError");
                    return;
                }
            }

            // Get the properties of the model and entity
            var modelProperties = typeof(TModel).GetProperties();
            var entityProperties = typeof(TEFEntity).GetProperties()
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            // Handle special properties
            HandleSpecialProperties(model, context);

            // Determine if this is a new entity or an existing one being updated
            // For a new entity, key properties will have default values
            // For an existing entity, at least one key property will have a non-default value
            var keyProperties = GetEntityKeyProperties(typeof(TEFEntity)).ToList();
            bool isNewEntity = true;
            
            if (keyProperties.Any())
            {
                foreach (var keyProperty in keyProperties)
                {
                    var keyValue = keyProperty.GetValue(dbEntity);
                    if (keyValue != null && !Equals(keyValue, GetDefaultValue(keyProperty.PropertyType)))
                    {
                        isNewEntity = false;
                        break;
                    }
                }
            }

            // Process each model property
            foreach (var modelProperty in modelProperties)
            {
                try
                {
                    // Skip properties marked with SkipMapping attribute (SkipToDb = true)
                    var skipAttribute = modelProperty.GetAttribute<SkipMappingAttribute>();
                    if (skipAttribute?.SkipToDb == true)
                        continue;

                    // Skip ID properties
                    if (keyProperties.Any(kp => kp.Name.Equals(modelProperty.Name, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    // For existing entities (updates), skip ID properties, sort index properties, and save date properties
                    if (!isNewEntity)
                    {
                        // Skip sort properties
                        if (modelProperty.GetAttribute<SortIndexAttribute>() != null)
                            continue;

                        // Skip save date properties
                        if (modelProperty.GetAttribute<SaveDateAttribute>() != null)
                            continue;

                        // Skip properties that were not included in the original request
                        // This preserves existing database values for properties not in the request
                        if (context.IncludedProperties != null && 
                            !context.IncludedProperties.Contains(modelProperty.Name))
                            continue;
                    }

                    // Get the model property value
                    var modelValue = modelProperty.GetValue(model);

                    // Skip null values if specified
                    if (modelValue == null && skipAttribute?.SkipNullValues == true)
                        continue;

                    // Try to find the corresponding entity property
                    if (!entityProperties.TryGetValue(modelProperty.Name, out var entityProperty) &&
                        (dbEntity is not ExpandoObject))
                    {
                        // Add error if property is missing in entity
                        string propPath = GetNestedPropertyPath(propertyPath, modelProperty.Name);
                            
                        AddValidationError(validationErrors, propPath, 
                            $"Property '{modelProperty.Name}' not found in entity '{typeof(TEFEntity).Name}'");
                        continue;
                    }

                    // Handle complex types
                    var complexAttribute = modelProperty.GetAttribute<BaseComplexAttribute>();
                    if (complexAttribute != null)
                    {
                        // Skip complex processing for simple types
                        if (IsSimpleType(complexAttribute.Type))
                        {
                            // Simple types are ALWAYS json.
                            SetPropertyValue(dbEntity, entityProperty,JsonConvert.SerializeObject(modelValue), modelProperty.Name);
                        }
                        else {
                            string nestedPropertyPath = GetNestedPropertyPath(propertyPath, modelProperty.Name);
                                
                            HandleComplexTypeToDb(modelProperty, entityProperty, modelValue, dbEntity, context, validationErrors, nestedPropertyPath);
                        }
                    }
                    else
                    {
                        // Get the field type attribute
                        var fieldTypeAttribute = GetFieldTypeAttribute(modelProperty);
                        if (fieldTypeAttribute != null)
                        {
                            // Validate and prepare the value with the field type plugin
                            string propPath = GetNestedPropertyPath(propertyPath, modelProperty.Name);

                            if (!ValidateAndPrepareForDb(fieldTypeAttribute, modelProperty, (dynamic) modelValue,
                                    entityProperty,
                                    dbEntity, context, validationErrors, propPath))
                            {
                                // Validation failed, but we continue processing other properties
                                continue;
                            }
                        }
                        else
                        {
                            // Handle simple types directly
                            SetPropertyValue(dbEntity, entityProperty,
                                ConvertValueIfNeeded(modelValue, entityProperty?.PropertyType), modelProperty.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Add error to validation errors
                    string propPath = GetNestedPropertyPath(propertyPath, modelProperty.Name);
                        
                    AddValidationError(validationErrors, propPath, ex.Message);
                }
            }

            // Call post-mapping after the main mapping is done
            if (model is BaseAdminModel baseAdminModelPost)
            {
                try
                {
                    baseAdminModelPost.CustomPostMapToDbModel(model, dbEntity, context);
                }
                catch (Exception ex)
                {
                    AddValidationError(validationErrors, "CustomMapping", 
                        $"Error in post-mapping: {ex.Message}", 
                        "CustomMappingError");
                }
            }
        }

        /// <summary>
        /// Handles special properties like save date and update date.
        /// </summary>
        private static void HandleSpecialProperties(object model, ModelMappingContext context)
        {
            if (model == null || context == null)
                return;

            var modelType = model.GetType();

            // Handle save date
            var saveDateProperty = modelType.GetPropertyWithAttribute<SaveDateAttribute>();
            if (saveDateProperty != null && IsDateTimeType(saveDateProperty.PropertyType))
            {
                var currentValue = saveDateProperty.GetValue(model);
                if (currentValue == null || (currentValue is DateTime dateValue && dateValue == default))
                {
                    saveDateProperty.SetValue(model, DateTime.UtcNow);
                }
            }

            // Handle update date - always update
            var updateDateProperty = modelType.GetPropertyWithAttribute<LastUpdateDateAttribute>();
            if (updateDateProperty != null && IsDateTimeType(updateDateProperty.PropertyType))
            {
                updateDateProperty.SetValue(model, DateTime.UtcNow);
            }

            // Handle updated by
            var updatedByProperty = modelType.GetPropertyWithAttribute<UpdatedByAttribute>();
            if (updatedByProperty != null && context.CurrentUserId != null)
            {
                // Use the ConvertValueIfNeeded method to handle the conversion to the appropriate type
                var convertedValue = ConvertValueIfNeeded(context.CurrentUserId, updatedByProperty.PropertyType);
                updatedByProperty.SetValue(model, convertedValue);
            }
        }

        /// <summary>
        /// Handles complex type mapping from database entity to model.
        /// </summary>
        private static void HandleComplexTypeFromDb<TModel>(PropertyInfo modelProperty, dynamic dbValue, 
            TModel model, ModelMappingContext context, Dictionary<string, List<DinoAdminConvertError>> validationErrors, 
            string propertyPath)
        {
            if (dbValue == null)
                return;
                
            var complexAttribute = modelProperty.GetAttribute<BaseComplexAttribute>();

            try
            {
                // First check if it's a repeater type
                if (complexAttribute is RepeaterAttribute)
                {
                    // Prepare for data based on storage type
                    IEnumerable itemsToProcess = null;

                    // For JSON storage
                    if (complexAttribute.StoreAsJson && dbValue is string json)
                    {
                        if (string.IsNullOrWhiteSpace(json))
                            return;

                        // Check if this is a repeater of simple types
                        if (IsSimpleType(complexAttribute.Type))
                        {
                            // For simple types, deserialize directly
                            var listType = typeof(List<>).MakeGenericType(complexAttribute.Type);
                            var modelItems = Activator.CreateInstance(listType) as IList;

                            try
                            {
                                var items = JsonConvert.DeserializeObject(json, typeof(List<>).MakeGenericType(complexAttribute.Type)) as IEnumerable;

                                if (items != null)
                                {
                                    foreach (var item in items)
                                    {
                                        modelItems.Add(item);
                                    }
                                }

                                // Set the simple list directly and we're done
                                SetPropertyValue(model, modelProperty, modelItems, modelProperty.Name);
                                return;
                            }
                            catch (Exception ex)
                            {
                                AddValidationError(validationErrors, propertyPath,
                                    $"Error deserializing simple type repeater: {ex.Message}", "JsonDeserializationError");
                                return;
                            }
                        }
                        else
                        {
                            // For complex types in JSON, deserialize to get items to process
                            itemsToProcess = JsonConvert.DeserializeObject<List<ExpandoObject>>(json);
                        }
                    }
                    // For related entity storage
                    else if (complexAttribute.RelatedEntity != null && dbValue is IEnumerable dbCollection)
                    {
                        // Process the collection directly
                        itemsToProcess = dbCollection;
                    }
                    // If it's ExpandoObject, it means we are in inner-json processing.
                    else if (dbValue is IEnumerable<object> expandoList)
                    {
                        itemsToProcess = expandoList;
                    }

                    // If we have items to process (from either JSON or related entities)
                    if (itemsToProcess != null)
                    {
                        // Use the unified process method for repeater items
                        var modelList = ProcessRepeaterItemsFromDb(
                            itemsToProcess,
                            complexAttribute,
                            context,
                            validationErrors,
                            propertyPath);

                        // Set the list to the model property
                        SetPropertyValue(model, modelProperty, modelList, modelProperty.Name);
                    }
                }
                // Complex.
                else
                {
                    // Handle single complex object
                    object modelInstance = null;
                    
                    if (complexAttribute.StoreAsJson && dbValue is string json)
                    {
                        if (string.IsNullOrWhiteSpace(json))
                            return;
                            
                        // Check if the complex attribute type is a simple type
                        if (IsSimpleType(complexAttribute.Type))
                        {
                            // For simple types, deserialize directly
                            var simpleValue = JsonConvert.DeserializeObject(json, complexAttribute.Type);
                            SetPropertyValue(model, modelProperty, simpleValue, modelProperty.Name);
                            return;
                        }

                        // For complex types, deserialize
                        modelInstance = JsonConvert.DeserializeObject<ExpandoObject>(json);
                    }
                    else if (dbValue is ExpandoObject expando)
                    {
                        modelInstance = expando;
                    }
                    else if (complexAttribute.RelatedEntity != null)
                    {
                        modelInstance = dbValue;
                    }
                    
                    // Process the model instance if we have one
                    if (modelInstance != null)
                    {
                        var processedItem = ProcessSingleComplexItemFromDb(
                            modelInstance,
                            complexAttribute,
                            context,
                            validationErrors,
                            propertyPath);
                            
                        SetPropertyValue(model, modelProperty, processedItem ?? modelInstance, modelProperty.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                AddValidationError(validationErrors, propertyPath, 
                    $"Error mapping complex type: {ex.Message}", "ComplexTypeMappingError");
            }
        }

        /// <summary>
        /// Handles complex type mapping from model to database entity.
        /// </summary>
        private static void HandleComplexTypeToDb(PropertyInfo modelProperty, PropertyInfo entityProperty, object modelValue,
            object dbEntity, ModelMappingContext context, Dictionary<string, List<DinoAdminConvertError>> validationErrors,
            string propertyPath)
        {
            var complexAttribute = modelProperty.GetAttribute<BaseComplexAttribute>();
            
            // Set to null if model value is null
            if (modelValue == null)
            {
                SetPropertyValue(dbEntity, entityProperty, null, modelProperty.Name);
                return;
            }
            
            // Process based on whether it's a repeater or single complex object
            if (complexAttribute is RepeaterAttribute)
            {
                // Add the JSON flag for later (we'll process everything the same way first)
                bool storeAsJson = complexAttribute.StoreAsJson;
                
                if (modelValue is IEnumerable modelCollection)
                {
                    try
                    {
                        // Increment the JSON processing depth if we're storing as JSON
                        if (storeAsJson)
                        {
                            context.JsonProcessingDepth++;
                        }

                        // Process items in a unified way first
                        var processedItems = ProcessRepeaterItems(
                            modelCollection, 
                            complexAttribute, 
                            context, 
                            validationErrors, 
                            propertyPath,
                            dbEntity,
                            entityProperty);
                        
                        if (storeAsJson)
                        {
                            context.JsonProcessingDepth--;
                        }

                        // Then store the processed items based on storage type
                        if (storeAsJson)
                        {
                            // Store as JSON string
                            if (context.InInnerProcessingOfJson)
                            {
                                SetPropertyValue(dbEntity, entityProperty, processedItems, modelProperty.Name);
                            }
                            else
                            {
                                var json = JsonConvert.SerializeObject(processedItems);
                                SetPropertyValue(dbEntity, entityProperty, json, modelProperty.Name);
                            }
                        }
                        else
                        {
                            // Store as entity collection
                            StoreProcessedItemsInCollection(
                                processedItems, 
                                entityProperty, 
                                dbEntity, 
                                validationErrors, 
                                propertyPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorType = storeAsJson ? "JsonSerializationError" : "EntityCollectionError";
                        AddValidationError(validationErrors, propertyPath, 
                            $"Error processing items: {ex.Message}", errorType);
                    }
                }
                else
                {
                    // Model value is not an enumerable but should be
                    AddValidationError(validationErrors, propertyPath, 
                        $"Property '{modelProperty.Name}' should be a collection but is not", "InvalidTypeError");
                }
            }
            // Single complex
            else
            {
                // Handle single complex object
                try
                {
                    // Get existing DB entity if available
                    object existingDbItem = null;
                    
                    if (!complexAttribute.StoreAsJson && complexAttribute.RelatedEntity != null)
                    {
                        existingDbItem = entityProperty.GetValue(dbEntity);
                    }
                    
                    // Increment the JSON processing depth if we're storing as JSON
                    if (complexAttribute.StoreAsJson)
                    {
                        context.JsonProcessingDepth++;
                    }
                    
                    // Process the single complex item
                    var processedItem = ProcessSingleComplexItemToDb(
                        modelValue, 
                        complexAttribute, 
                        context, 
                        validationErrors, 
                        propertyPath,
                        existingDbItem);
                        
                    // Increment the JSON processing depth if we're storing as JSON
                    if (complexAttribute.StoreAsJson)
                    {
                        context.JsonProcessingDepth--;
                    }

                    if (processedItem != null)
                    {
                        // Now decide how to store the processed item based on storage type
                        if (complexAttribute.StoreAsJson)
                        {
                            if (context.InInnerProcessingOfJson)
                            {
                                SetPropertyValue(dbEntity, entityProperty, processedItem, modelProperty.Name);
                            }
                            else
                            {
                                var json = JsonConvert.SerializeObject(processedItem);
                                SetPropertyValue(dbEntity, entityProperty, json, modelProperty.Name);
                            }
                        }
                        else
                        {

                            // Store as entity directly
                            SetPropertyValue(dbEntity, entityProperty, processedItem, modelProperty.Name);
                        }
                    }
                    else
                    {
                        // Set to null if processing returned null
                        SetPropertyValue(dbEntity, entityProperty, null, modelProperty.Name);
                    }
                }
                catch (Exception ex)
                {
                    AddValidationError(validationErrors, propertyPath, 
                        $"Error processing complex object: {ex.Message}", "ComplexObjectProcessingError");
                }
            }
        }

        /// <summary>
        /// Processes repeater items in a unified way regardless of storage type.
        /// </summary>
        private static List<object> ProcessRepeaterItems(
            IEnumerable modelCollection,
            BaseComplexAttribute complexAttribute,
            ModelMappingContext context,
            Dictionary<string, List<DinoAdminConvertError>> validationErrors,
            string propertyPath,
            object dbEntity,
            PropertyInfo entityProperty)
        {
            var processedItems = new List<object>();
            
            int index = 0;
            foreach (var item in modelCollection)
            {
                if (item == null)
                {
                    index++;
                    continue;
                }
                
                string itemPath = $"{propertyPath}[{index}]";
                
                try
                {
                    // Skip complex processing for simple types
                    if (IsSimpleType(complexAttribute.Type))
                    {
                        processedItems.Add(item);
                        index++;
                        continue;
                    }

                    // Try to find an existing DB entity if we have a non-JSON collection
                    object existingDbItem = null;
                    
                    if (!complexAttribute.StoreAsJson && complexAttribute.RelatedEntity != null)
                    {
                        // Get the current database collection if it exists
                        var dbCollection = entityProperty.GetValue(dbEntity) as IEnumerable;
                        
                        if (dbCollection != null)
                        {
                            existingDbItem = FindExistingEntityByKeys(dbCollection, item, complexAttribute.RelatedEntity);
                        }
                    }
                    
                    // Process the complex item
                    var processedItem = ProcessSingleComplexItemToDb(
                        item, 
                        complexAttribute, 
                        context, 
                        validationErrors, 
                        itemPath,
                        existingDbItem);
                        
                    if (processedItem != null)
                    {
                        // Handle sort index on the processed item if needed
                        SetSortIndex(processedItem, index);
                        
                        processedItems.Add(processedItem);
                    }
                }
                catch (Exception ex)
                {
                    AddValidationError(validationErrors, itemPath, 
                        $"Error processing complex item: {ex.Message}", "ComplexItemProcessingError");
                }
                
                index++;
            }
            
            return processedItems;
        }

        /// <summary>
        /// Stores processed items in a collection property.
        /// </summary>
        private static void StoreProcessedItemsInCollection(
            List<object> processedItems,
            PropertyInfo entityProperty,
            object dbEntity,
            Dictionary<string, List<DinoAdminConvertError>> validationErrors,
            string propertyPath)
        {
            // Get or create the collection
            var dbCollection = entityProperty.GetValue(dbEntity);
            var collectionType = entityProperty.PropertyType;
            var dbListInstance = dbCollection ?? Activator.CreateInstance(collectionType);
            
            // Non-generic IList is widely implemented and works for almost all collection types
            if (dbListInstance is IList nonGenericList)
            {
                nonGenericList.Clear();
                
                foreach (var item in processedItems)
                {
                    nonGenericList.Add(item);
                }
            }
            else
            {
                // Fallback to dynamic invocation for custom collections
                var addMethod = collectionType.GetMethod("Add");
                var clearMethod = collectionType.GetMethod("Clear");
                
                if (clearMethod != null && addMethod != null)
                {
                    clearMethod.Invoke(dbListInstance, null);
                    
                    foreach (var item in processedItems)
                    {
                        addMethod.Invoke(dbListInstance, new[] { item });
                    }
                }
                else
                {
                    // If we can't clear/add, log a validation error
                    AddValidationError(validationErrors, propertyPath,
                        "Collection type doesn't support required operations (Add/Clear)",
                        "UnsupportedCollectionType");
                }
            }
            
            // Set the collection back to the entity property
            SetPropertyValue(dbEntity, entityProperty, dbListInstance, null);
        }

        /// <summary>
        /// Processes a single complex item, converting it to the appropriate entity type.
        /// </summary>
        /// <param name="modelItem">The admin model item to process</param>
        /// <param name="complexAttribute">The complex attribute with type information</param>
        /// <param name="context">The mapping context</param>
        /// <param name="validationErrors">Dictionary to collect validation errors</param>
        /// <param name="propertyPath">Current property path for error reporting</param>
        /// <param name="existingDbItem">Optional existing database item to update</param>
        /// <returns>The processed entity item or null if processing failed</returns>
        private static object ProcessSingleComplexItemToDb(
            object modelItem, 
            BaseComplexAttribute complexAttribute, 
            ModelMappingContext context,
            Dictionary<string, List<DinoAdminConvertError>> validationErrors, 
            string propertyPath,
            object existingDbItem = null)
        {
            if (modelItem == null)
                return null;
            
            try
            {
                // Get the base type and its generic parameters
                var modelType = modelItem.GetType();

                if (modelType != null)
                {
                    // For JSON storage, we still need to call ToDbEntityFromTypes to run validation
                    // and custom mapping logic
                    var entityTypeArg = complexAttribute.StoreAsJson ? typeof(ExpandoObject) : complexAttribute.RelatedEntity;
                    
                    if (complexAttribute.RelatedEntity != null)
                    {
                        // Get or create the related entity
                        var relatedEntityType = complexAttribute.RelatedEntity;
                        
                        // Use existing DB entity if provided, otherwise create a new one
                        object relatedEntity = existingDbItem ?? Activator.CreateInstance(relatedEntityType);

                        // Call ToDbEntityFromTypes with the model type
                        var result = ToDbEntityFromTypes(modelItem, modelType, entityTypeArg, context, relatedEntity);
                        return result.Item1;
                    }
                    else if (complexAttribute.StoreAsJson)
                    {
                        // Get existing JSON data if available
                        string existingJsonData = null;
                        if (existingDbItem is string jsonString)
                        {
                            existingJsonData = jsonString;
                        }

                        // Create a temporary entity for validation, initialized with model properties
                        // and any existing data
                        var tempEntity = ExpandoObjectExtensions.CreateFrom(modelItem, existingJsonData);

                        // Call ToDbEntityFromTypes with the model type for validation and processing
                        var result = ToDbEntityFromTypes(modelItem, modelType, entityTypeArg, context, tempEntity);
                        
                        // Check if there were any validation errors
                        if (result.Item2?.ValidationErrors != null && result.Item2.ValidationErrors.Count > 0)
                        {
                            foreach (var kvp in result.Item2.ValidationErrors)
                            {
                                foreach (var error in kvp.Value)
                                {
                                    AddValidationError(validationErrors, 
                                        $"{propertyPath}.{error.PropertyPath}", 
                                        error.Message, 
                                        error.ErrorCode);
                                }
                            }
                        }

                        // Return the ExpandoObject directly
                        return result.Item1;
                    }
                    else
                    {
                        // Something is wrong - either RelatedEntity or StoreAsJson should be set
                        AddValidationError(validationErrors, propertyPath, 
                            "Complex attribute must specify either RelatedEntity or StoreAsJson", 
                            "InvalidComplexAttributeError");
                        return null;
                    }
                }
                else
                {
                    AddValidationError(validationErrors, propertyPath, 
                        "Model type must inherit from BaseAdminModel", 
                        "InvalidModelTypeError");
                    return null;
                }
            }
            catch (Exception ex)
            {
                AddValidationError(validationErrors, propertyPath, 
                    $"Error processing complex item: {ex.Message}", 
                    "ComplexItemProcessingError");
                return null;
            }
        }

        /// <summary>
        /// Gets the field type attribute for a property.
        /// </summary>
        private static AdminFieldBaseAttribute GetFieldTypeAttribute(PropertyInfo property)
        {
            // Find first attribute that inherits from AdminFieldBaseAttribute
            return property.GetCustomAttributes(true)
                .FirstOrDefault(a => a is AdminFieldBaseAttribute) as AdminFieldBaseAttribute;
        }

        /// <summary>
        /// Validates and prepares a value for database storage using the appropriate field type plugin.
        /// </summary>
        private static bool ValidateAndPrepareForDb(AdminFieldBaseAttribute fieldTypeAttribute, PropertyInfo modelProperty, 
            dynamic modelValue, PropertyInfo entityProperty, object dbEntity, ModelMappingContext context, 
            Dictionary<string, List<DinoAdminConvertError>> validationErrors, string propertyPath)
        {
            var plugin = context.PluginRegistry.GetPlugin(fieldTypeAttribute);
            if (plugin == null)
                return false;

            try
            {
                // Validate the value
                (bool IsValid, List<string> ErrorMessages) validationResult = plugin.Validate(modelValue, modelProperty);
                
                // Check validation result
                if (!validationResult.IsValid)
                {
                    // Add validation errors
                    foreach (var error in validationResult.ErrorMessages)
                    {
                        AddValidationError(validationErrors, propertyPath, error, "FieldValidationError");
                    }
                    
                    return false;
                }

                // Prepare the value for database storage.
                var dbValue = plugin.PrepareForDb(modelValue, modelProperty);

                // Handle multi-select relationships
                if (fieldTypeAttribute is AdminFieldMultiSelectAttribute multiSelectAttr && !multiSelectAttr.StoreAsJson)
                {
                    return HandleMultiSelectRelationship(
                        multiSelectAttr,
                        dbValue, // Use the prepared value from the plugin
                        entityProperty,
                        dbEntity,
                        context,
                        validationErrors,
                        propertyPath);
                }
                
                // For multi-select or JSON storage, set the prepared value
                SetPropertyValue(dbEntity, entityProperty, ConvertValueIfNeeded(dbValue, entityProperty?.PropertyType), modelProperty.Name);
                return true;
            }
            catch (Exception ex)
            {
                AddValidationError(validationErrors, propertyPath, 
                    $"Error processing field for database: {ex.Message}", "FieldProcessingError");
                return false;
            }
        }

        /// <summary>
        /// Handles the management of multi-select relationships.
        /// Supports two scenarios:
        /// 1. Junction table with ID column: Collection contains junction entities.
        /// 2. Direct many-to-many (no junction entity): Collection contains target entities directly, with no ID column.
        /// </summary>
        private static bool HandleMultiSelectRelationship(
            AdminFieldMultiSelectAttribute attribute,
            object modelValue,
            PropertyInfo entityProperty,
            object dbEntity,
            ModelMappingContext context,
            Dictionary<string, List<DinoAdminConvertError>> validationErrors,
            string propertyPath)
        {
            // Get and validate collection property
            var collectionProperty = entityProperty.DeclaringType.GetProperty(
                attribute.RelatedCollectionPropertyName ??
                entityProperty.Name);
            if (collectionProperty == null)
            {
                AddValidationError(validationErrors, propertyPath, 
                    $"Collection property '{attribute.RelatedCollectionPropertyName}' not found", 
                    "MissingCollectionPropertyError");
                return false;
            }

            // Get the collection's element type
            var collectionElementType = collectionProperty.PropertyType.GetGenericArguments()[0];
            
            // Get the property that holds the related entity ID.
            var relatedIdPropertyName = attribute.RelatedEntityIdProperty ?? "Id";
            var relatedIdProperty = collectionElementType.GetProperty(relatedIdPropertyName);
            
            // Get the primary key properties of the collection element type
            var keyProperties = GetEntityKeyProperties(collectionElementType).ToList();

            // Determine which scenario we're in:
            // Scenario #2: Junction entity with separate ID - relatedIdProperty exists and is different from primary key
            // Scenario #3: Direct many-to-many - relatedIdProperty IS the primary key (or doesn't exist)
            bool isJunctionEntity = relatedIdProperty != null && 
                                    keyProperties.Any() && 
                                    relatedIdProperty.Name != keyProperties[0].Name;

            // Validate and convert selected IDs
            var selectedIds = modelValue as IEnumerable<object>;
            if (selectedIds == null)
            {
                AddValidationError(validationErrors, propertyPath, 
                    "Selected values must be a collection", 
                    "InvalidValueTypeError");
                return false;
            }
            
            var selectedIdsList = selectedIds.ToList();

            try
            {
                // Get current collection
                var currentCollection = collectionProperty.GetValue(dbEntity) as IEnumerable;
                var currentItems = currentCollection?.Cast<object>().ToList() ?? new List<object>();

                // Create and populate new collection
                var collectionType = typeof(List<>).MakeGenericType(collectionElementType);
                var newCollection = Activator.CreateInstance(collectionType) as IList;
                if (newCollection == null)
                {
                    AddValidationError(validationErrors, propertyPath, 
                        "Failed to create collection", 
                        "CollectionCreationError");
                    return false;
                }

                if (isJunctionEntity)
                {
                    // Scenario #2: Junction table with explicit entity and ID column
                    // Find existing junction entities by RelatedEntityIdProperty, not by primary key
                    foreach (var selectedId in selectedIdsList)
                    {
                        var convertedId = ConvertIdToPropertyType(selectedId, relatedIdProperty.PropertyType);
                        
                        // Try to find existing junction entity where RelatedEntityIdProperty matches
                        var existingJunction = currentItems.FirstOrDefault(item =>
                        {
                            var itemIdValue = relatedIdProperty.GetValue(item);
                            return Equals(itemIdValue, convertedId);
                        });

                        if (existingJunction != null)
                        {
                            // Keep existing junction entity
                            newCollection.Add(existingJunction);
                        }
                        else
                        {
                            // Create new junction entity with RelatedEntityIdProperty set
                            var newJunction = Activator.CreateInstance(collectionElementType);
                            relatedIdProperty.SetValue(newJunction, convertedId);
                            newCollection.Add(newJunction);
                        }
                    }
                }
                else
                {
                    // Scenario #3: Direct many-to-many (no junction entity)
                    // Collection contains target entities directly
                    // We must find existing entities, NOT create new ones
                    
                    var targetKeyProperty = keyProperties.FirstOrDefault() ?? relatedIdProperty;
                    if (targetKeyProperty == null)
                    {
                        AddValidationError(validationErrors, propertyPath, 
                            "No key property found on target entity", 
                            "MissingKeyPropertyError");
                        return false;
                    }

                    foreach (var selectedId in selectedIdsList)
                    {
                        var convertedId = ConvertIdToPropertyType(selectedId, targetKeyProperty.PropertyType);
                        
                        // Try to find in current collection first
                        var existingEntity = currentItems.FirstOrDefault(item =>
                        {
                            var itemIdValue = targetKeyProperty.GetValue(item);
                            return Equals(itemIdValue, convertedId);
                        });

                        if (existingEntity != null)
                        {
                            // Keep existing entity from collection
                            newCollection.Add(existingEntity);
                        }
                        else if (context.DbContext != null)
                        {
                            // Try to find entity from DbContext
                            var entityFromDb = FindEntityFromDbContext(context.DbContext, collectionElementType, convertedId);
                            if (entityFromDb != null)
                            {
                                newCollection.Add(entityFromDb);
                            }
                            else
                            {
                                AddValidationError(validationErrors, propertyPath, 
                                    $"Entity with ID '{selectedId}' not found in database", 
                                    "EntityNotFoundError");
                                return false;
                            }
                        }
                        else
                        {
                            AddValidationError(validationErrors, propertyPath, 
                                "DbContext is required for direct many-to-many relationships", 
                                "DbContextRequiredError");
                            return false;
                        }
                    }
                }

                // Set the new collection to the entity property
                collectionProperty.SetValue(dbEntity, newCollection);
                return true;
            }
            catch (Exception ex)
            {
                AddValidationError(validationErrors, propertyPath, 
                    $"Error managing multi-select relationship: {ex.Message}", 
                    "RelationshipManagementError");
                return false;
            }
        }
        
        /// <summary>
        /// Handles the extraction of multi-select relationship values from DB to model.
        /// Symmetric with HandleMultiSelectRelationship (Model → DB direction).
        /// Extracts IDs from relationship collections based on RelatedEntityIdProperty.
        /// </summary>
        private static void HandleMultiSelectRelationshipFromDb(
            AdminFieldMultiSelectAttribute attribute,
            object dbValue,
            PropertyInfo modelProperty,
            object model,
            Dictionary<string, List<DinoAdminConvertError>> validationErrors,
            string propertyPath)
        {
            try
            {
                var result = new List<object>();
                
                if (dbValue == null)
                {
                    SetPropertyValue(model, modelProperty, result, modelProperty.Name);
                    return;
                }
                
                // Check if it's a collection
                if (!(dbValue is IEnumerable enumerable))
                {
                    SetPropertyValue(model, modelProperty, result, modelProperty.Name);
                    return;
                }
                
                // Get the property that holds the related entity ID
                var relatedIdPropertyName = attribute.RelatedEntityIdProperty ?? "Id";
                PropertyInfo idProperty = null;
                
                foreach (var item in enumerable)
                {
                    if (item == null)
                        continue;
                    
                    // Get the property info on first iteration
                    if (idProperty == null)
                    {
                        var itemType = item.GetType();
                        
                        // Get primary key properties to determine scenario
                        var keyProperties = GetEntityKeyProperties(itemType).ToList();
                        idProperty = itemType.GetProperty(relatedIdPropertyName);
                        
                        // Determine scenario: junction entity vs direct many-to-many
                        bool isJunctionEntity = idProperty != null && 
                                               keyProperties.Any() && 
                                               idProperty.Name != keyProperties[0].Name;
                        
                        if (isJunctionEntity)
                        {
                            // Scenario #2: Use RelatedEntityIdProperty (e.g., BreakerActionTypeId)
                            // idProperty is already set correctly
                        }
                        else
                        {
                            // Scenario #3: Direct many-to-many, use the primary key
                            idProperty = keyProperties.FirstOrDefault() ?? idProperty;
                        }
                        
                        // Fallback: try "Id" if nothing found
                        if (idProperty == null)
                        {
                            idProperty = itemType.GetProperty("Id");
                        }
                        
                        if (idProperty == null)
                        {
                            AddValidationError(validationErrors, propertyPath,
                                $"Could not find ID property '{relatedIdPropertyName}' on type '{itemType.Name}'",
                                "MissingIdPropertyError");
                            return;
                        }
                    }
                    
                    var idValue = idProperty.GetValue(item);
                    if (idValue != null)
                    {
                        result.Add(idValue);
                    }
                }
                
                // Convert to the appropriate type for the model property
                var convertedValue = ConvertValueIfNeeded(result, modelProperty.PropertyType);
                SetPropertyValue(model, modelProperty, convertedValue, modelProperty.Name);
            }
            catch (Exception ex)
            {
                AddValidationError(validationErrors, propertyPath,
                    $"Error extracting multi-select values: {ex.Message}",
                    "MultiSelectExtractionError");
            }
        }
        
        /// <summary>
        /// Converts an ID value to the target property type.
        /// </summary>
        private static object ConvertIdToPropertyType(object id, Type targetType)
        {
            if (id == null) return null;
            
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            
            if (id.GetType() == underlyingType)
                return id;
                
            return Convert.ChangeType(id, underlyingType);
        }
        
        /// <summary>
        /// Finds an entity from DbContext by its primary key.
        /// </summary>
        private static object FindEntityFromDbContext(object dbContext, Type entityType, object keyValue)
        {
            if (dbContext == null) return null;
            
            try
            {
                // Get the Set<T> method and invoke it
                var setMethod = dbContext.GetType().GetMethod("Set", Type.EmptyTypes);
                if (setMethod == null) return null;
                
                var genericSetMethod = setMethod.MakeGenericMethod(entityType);
                var dbSet = genericSetMethod.Invoke(dbContext, null);
                if (dbSet == null) return null;
                
                // Use Find method to get entity by key
                var findMethod = dbSet.GetType().GetMethod("Find", new[] { typeof(object[]) });
                if (findMethod != null)
                {
                    return findMethod.Invoke(dbSet, new object[] { new object[] { keyValue } });
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Adds a validation error to the collection.
        /// </summary>
        private static void AddValidationError(Dictionary<string, List<DinoAdminConvertError>> validationErrors, 
            string propertyPath, string message, string errorCode = "ValidationError")
        {
            if (!validationErrors.TryGetValue(propertyPath, out var errors))
            {
                errors = new List<DinoAdminConvertError>();
                validationErrors[propertyPath] = errors;
            }

            errors.Add(new DinoAdminConvertError
            {
                PropertyName = propertyPath.Contains(".") ? propertyPath.Substring(propertyPath.LastIndexOf('.') + 1) : propertyPath,
                PropertyPath = propertyPath,
                Message = message,
                ErrorCode = errorCode
            });
        }

        #region Path Helpers
        
        /// <summary>
        /// Creates a nested property path by combining a base path with a property name.
        /// </summary>
        /// <param name="basePath">The base path (can be empty)</param>
        /// <param name="propertyName">The property name to append</param>
        /// <returns>The combined property path</returns>
        private static string GetNestedPropertyPath(string basePath, string propertyName)
        {
            return string.IsNullOrEmpty(basePath) 
                ? propertyName 
                : $"{basePath}.{propertyName}";
        }
        
        #endregion

        #region DateType Checking Helpers
        
        /// <summary>
        /// Checks if a type is a DateTime type (including nullable DateTime).
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is DateTime or nullable DateTime</returns>
        private static bool IsDateTimeType(Type type)
        {
            return type == typeof(DateTime) || type == typeof(DateTime?);
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
        /// Checks if a type is a simple type (i.e., not a complex type).
        /// </summary>
        private static bool IsSimpleType(Type type)
        {
            // Handle nullable types (like int?, DateTime?, etc.)
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // If it's nullable, check the underlying type
                type = Nullable.GetUnderlyingType(type);
            }
            
            return type.IsPrimitive ||  // Covers bool, byte, char, etc.
                   type.IsEnum ||       // Enums are simple types
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid);
        }
        
        #endregion

        #region Value Conversion Helpers
        
        /// <summary>
        /// Gets the default value for a type.
        /// </summary>
        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
        
        /// <summary>
        /// Converts a value to the target type if needed.
        /// </summary>
        private static object ConvertValueIfNeeded(object value, Type targetType)
        {
            // If the target type is null, return the value as is. It might be that the original is ExpandoObject.
            if (targetType == null)
                return value;

            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (value.GetType() == targetType || targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(targetType);
                    var convertedValue = ConvertValueIfNeeded(value, underlyingType);
                    return convertedValue;
                }

                // Handle UrlFieldType conversion from JSON string
                if (targetType == typeof(UrlFieldType) && value is string jsonString && !string.IsNullOrEmpty(jsonString))
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<UrlFieldType>(jsonString);
                    }
                    catch (JsonException)
                    {
                        // If JSON deserialization fails, return a default instance
                        return new UrlFieldType();
                    }
                }

                // Handle numeric type conversions
                if (IsNumericType(value.GetType()) && IsNumericType(targetType))
                {
                    // Convert through decimal to handle all numeric types
                    var decimalValue = Convert.ToDecimal(value);
                    return Convert.ChangeType(decimalValue, targetType);
                }

                // Handle generic List types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = targetType.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var resultList = (IList)Activator.CreateInstance(listType);

                    // Handle if value is already IEnumerable
                    if (value is IEnumerable sourceList)
                    {
                        foreach (var item in sourceList)
                        {
                            if (item != null)
                            {
                                resultList.Add(ConvertValueIfNeeded(item, elementType));
                            }
                        }
                        return resultList;
                    }
                    
                    // Single value to list
                    resultList.Add(ConvertValueIfNeeded(value, elementType));
                    return resultList;
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
        
        #endregion

        #region Entity Key Helpers
        
        /// <summary>
        /// Gets the ID of an entity using reflection to find the primary key property.
        /// </summary>
        public static string GetEntityId<TEFEntity>(TEFEntity entity) where TEFEntity : class
        {
            if (entity == null)
                return null;
                
            // Use the GetEntityKeyProperties method to find primary key properties through reflection
            var keyProperties = GetEntityKeyProperties(typeof(TEFEntity));
            
            // If we found key properties, use the first one as the ID
            if (keyProperties.Any())
            {
                var keyProperty = keyProperties.First();
                var keyValue = keyProperty.GetValue(entity);
                return keyValue?.ToString();
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets the primary key properties for an entity type.
        /// </summary>
        public static IEnumerable<PropertyInfo> GetEntityKeyProperties(Type entityType)
        {
            return entityType.GetEntityKeyProperties();
        }
        
        /// <summary>
        /// Finds an existing entity by keys.
        /// </summary>
        private static object FindExistingEntityByKeys(IEnumerable collection, object item, Type relatedEntityType)
        {
            // Get the key properties for the entity type
            var keyProperties = GetEntityKeyProperties(relatedEntityType);
            
            // Get key values from the item
            var keyValues = GetEntityKeyValues(item, keyProperties);
            
            // If we have no key values, we can't find a match
            if (!keyValues.Any())
                return null;
                
            // Look through the collection for a matching entity
            foreach (var dbEntity in collection)
            {
                if (dbEntity == null)
                    continue;
                    
                bool isMatch = true;
                
                // Check all key values to see if they match
                foreach (var kv in keyValues)
                {
                    var dbValue = kv.Key.GetValue(dbEntity);
                    if (!Equals(dbValue, kv.Value))
                    {
                        isMatch = false;
                        break;
                    }
                }
                
                if (isMatch)
                {
                    return dbEntity;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets key-value pairs from an entity for the specified key properties.
        /// </summary>
        private static Dictionary<PropertyInfo, object> GetEntityKeyValues(object entity, IEnumerable<PropertyInfo> keyProperties)
        {
            var keyValues = new Dictionary<PropertyInfo, object>();
            
            if (entity == null)
                return keyValues;
                
            foreach (var keyProperty in keyProperties)
            {
                // Try to find a matching property on the entity
                var entityProperty = entity.GetType().GetProperty(keyProperty.Name);
                if (entityProperty != null)
                {
                    var keyValue = entityProperty.GetValue(entity);
                    if (keyValue != null && !Equals(keyValue, GetDefaultValue(entityProperty.PropertyType)))
                    {
                        keyValues.Add(keyProperty, keyValue);
                    }
                }
            }
            
            return keyValues;
        }
        
        #endregion

        #region Data Structure Helpers
        
        /// <summary>
        /// Sets the sort index on an object if it has a property with the SortIndexAttribute.
        /// </summary>
        /// <param name="item">The object to set the sort index on</param>
        /// <param name="index">The sort index value to set</param>
        /// <returns>True if the sort index was set, false otherwise</returns>
        public static bool SetSortIndex(object item, int index)
        {
            if (item == null)
                return false;
                
            var sortProperty = item.GetType().GetPropertyWithAttribute<SortIndexAttribute>();
            
            if (sortProperty != null && sortProperty.PropertyType == typeof(int))
            {
                sortProperty.SetValue(item, index);
                return true;
            }
            
            return false;
        }
        
        #endregion

        /// <summary>
        /// Processes repeater items from database or JSON to model objects
        /// </summary>
        private static object ProcessRepeaterItemsFromDb(IEnumerable dbItems, BaseComplexAttribute complexAttribute, 
            ModelMappingContext context, Dictionary<string, List<DinoAdminConvertError>> validationErrors, string propertyPath)
        {
            // Create the model list type
            var modelListType = typeof(List<>).MakeGenericType(complexAttribute.Type);
            var resultList = Activator.CreateInstance(modelListType) as IList;
            
            int index = 0;
            foreach (var item in dbItems)
            {
                if (item == null)
                {
                    index++;
                    continue;
                }
                
                string itemPath = $"{propertyPath}[{index}]";
                
                try
                {
                    // Skip complex processing for simple types
                    if (IsSimpleType(complexAttribute.Type))
                    {
                        resultList.Add(ConvertValueIfNeeded(item, complexAttribute.Type));
                        continue;
                    }

                    // Check if this is a related entity collection or JSON-deserialized collection
                    bool isRelatedEntity = complexAttribute.RelatedEntity != null && 
                                         complexAttribute.RelatedEntity.IsAssignableFrom(item.GetType());


                    object modelInstance;

                    if (isRelatedEntity)
                    {
                        // For related entities, use ToAdminModelFromTypes
                        var result = ToAdminModelFromTypes(item, complexAttribute.Type, complexAttribute.RelatedEntity, context);

                        modelInstance = result.Item1;
                            
                            // Add validation errors with proper path
                            if (result.Item2?.Count > 0)
                            {
                                foreach (var kvp in result.Item2)
                                {
                                    foreach (var error in kvp.Value)
                                    {
                                        AddValidationError(validationErrors, 
                                            $"{itemPath}.{error.PropertyPath}", 
                                            error.Message, 
                                            error.ErrorCode);
                                    }
                                }
                            }
                    }
                    else
                    {
                        modelInstance = item;
                    }


                    // Add to the list if we got a valid model
                    if (modelInstance != null)
                    {
                        // Process the model instance using ProcessSingleComplexItemFromDb to ensure 
                        // consistent processing of field types, nested complex properties, etc.
                        // IMPORTANT: For related entities, we've already called ToAdminModelFromTypes above,
                        // so we shouldn't process it again with another ToAdminModelFromTypes call
                        object processedItem;
                        
                        if (isRelatedEntity)
                        {
                            // For related entities, we can just use the model instance directly
                            // since it's already been fully processed by ToAdminModelFromTypes
                            processedItem = modelInstance;
                        }
                        else
                        {
                            // For JSON-deserialized items, we need to process them
                            processedItem = ProcessSingleComplexItemFromDb(
                                modelInstance,
                                complexAttribute,
                                context,
                                validationErrors,
                                itemPath);
                        }

                        if (processedItem != null)
                        {
                            resultList.Add(processedItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorType = complexAttribute.RelatedEntity != null ? "RelatedEntityProcessingError" : "JsonProcessingError";
                    AddValidationError(validationErrors, itemPath, 
                        $"Error processing item: {ex.Message}", errorType);
                }
                
                index++;
            }
            
            return resultList;
        }

        /// <summary>
        /// Processes a single complex item from database (or JSON) to admin model.
        /// </summary>
        /// <param name="dbItem">The database or JSON-deserialized item to process</param>
        /// <param name="complexAttribute">The complex attribute with type information</param>
        /// <param name="context">The mapping context</param>
        /// <param name="validationErrors">Dictionary to collect validation errors</param>
        /// <param name="propertyPath">Current property path for error reporting</param>
        /// <returns>The processed admin model item or null if processing failed</returns>
        private static object ProcessSingleComplexItemFromDb(
            object dbItem, 
            BaseComplexAttribute complexAttribute, 
            ModelMappingContext context,
            Dictionary<string, List<DinoAdminConvertError>> validationErrors, 
            string propertyPath)
        {
            if (dbItem == null)
                return null;
            
            try
            {
                // For simple types, just return the item
                if (IsSimpleType(complexAttribute.Type))
                {
                    return ConvertValueIfNeeded(dbItem, complexAttribute.Type);
                }
                
                // For complex types, create an instance of the model type
                var modelType = complexAttribute.Type;
                var modelInstance = Activator.CreateInstance(modelType);
                
                if (modelType != null)
                {
                    // Handle BaseAdminModel types
                    var entityTypeArg = complexAttribute.StoreAsJson ? typeof(ExpandoObject) : complexAttribute.RelatedEntity;

                    // Check if the db item is compatible with the entity type
                    if (!complexAttribute.StoreAsJson && entityTypeArg.IsAssignableFrom(dbItem.GetType()))
                    {
                        // Use the ToAdminModelFromTypes extension method
                        var result = ToAdminModelFromTypes(dbItem, modelType, entityTypeArg, context);

                        modelInstance = result.Item1;
                            
                        // Add validation errors with proper path
                        if (result.Item2?.Count > 0)
                        {
                            foreach (var kvp in result.Item2)
                            {
                                foreach (var error in kvp.Value)
                                {
                                    AddValidationError(validationErrors, 
                                        $"{propertyPath}.{error.PropertyPath}", 
                                        error.Message, 
                                        error.ErrorCode);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Handle JSON-deserialized items for BaseAdminModel types
                        MapPropertiesFromJsonToModel((ExpandoObject)dbItem, modelInstance, context, validationErrors, propertyPath);
                    }
                }
                else
                {
                    // Handle simple complex types (like ItemProperty)
                    MapPropertiesFromJsonToModel((ExpandoObject)dbItem, modelInstance, context, validationErrors, propertyPath);
                }
                
                return modelInstance;
            }
            catch (Exception ex)
            {
                AddValidationError(validationErrors, propertyPath, 
                    $"Error processing complex item from DB: {ex.Message}", 
                    "ComplexItemProcessingError");
                return null;
            }
        }

        /// <summary>
        /// Maps properties from a JSON-deserialized object to a model instance.
        /// </summary>
        public static void MapPropertiesFromJsonToModel(
            ExpandoObject source,
            object target,
            ModelMappingContext context,
            Dictionary<string, List<DinoAdminConvertError>> validationErrors,
            string propertyPath)
        {
            var modelProps = target.GetType().GetProperties();
            var sourceDic = (IDictionary<string, object>)source;

            foreach (var modelProp in modelProps)
            {
                try
                {
                    // Skip properties marked with SkipMapping attribute (SkipFromDb = true)
                    var skipAttribute = modelProp.GetAttribute<SkipMappingAttribute>();
                    if (skipAttribute?.SkipFromDb == true)
                        continue;
                        
                    // Try to find the corresponding property
                    if (sourceDic.TryGetValue(modelProp.Name, out var sourceValue))
                    {
                        // Get the field type attribute
                        var fieldTypeAttribute = GetFieldTypeAttribute(modelProp);
                        if (fieldTypeAttribute != null)
                        {
                            // Use field type plugin to prepare the value for the model
                            var plugin = context.PluginRegistry.GetPlugin(fieldTypeAttribute);
                            if (plugin != null)
                            {
                                try
                                {
                                    // Use dynamic to call PrepareForModel
                                    var preparedValue = plugin.PrepareForModel((dynamic)sourceValue, modelProp);
                                    
                                    // Convert and set the value
                                    var convertedValue = ConvertValueIfNeeded(preparedValue, modelProp.PropertyType);
                                    SetPropertyValue(target, modelProp, convertedValue, modelProp.Name);
                                }
                                catch (Exception ex)
                                {
                                    string propPath = $"{propertyPath}.{modelProp.Name}";
                                    
                                    AddValidationError(validationErrors, propPath, 
                                        $"Error preparing field: {ex.Message}", 
                                        "FieldTypePluginError");
                                }
                                continue;
                            }
                        }
                        
                        // Handle complex properties
                        var complexAttr = modelProp.GetAttribute<BaseComplexAttribute>();
                        if (complexAttr != null)
                        {
                            string nestedPropertyPath = $"{propertyPath}.{modelProp.Name}";
                            
                            HandleComplexTypeFromDb(modelProp, sourceValue, target, 
                                context, validationErrors, nestedPropertyPath);
                        }
                        else
                        {
                            // Handle simple types directly
                            try
                            {
                                SetPropertyValue(target, modelProp, ConvertValueIfNeeded(sourceValue, modelProp.PropertyType), modelProp.Name);
                            }
                            catch (Exception ex)
                            {
                                string propPath = $"{propertyPath}.{modelProp.Name}";
                                    
                                AddValidationError(validationErrors, propPath, 
                                    $"Error converting value: {ex.Message}", 
                                    "ValueConversionError");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string propPath = $"{propertyPath}.{modelProp.Name}";
                    
                    AddValidationError(validationErrors, propPath, 
                        $"Error mapping property: {ex.Message}", 
                        "PropertyMappingError");
                }
            }
        }

        /// <summary>
        /// Sets a property value on either a regular entity/model or an ExpandoObject.
        /// </summary>
        /// <param name="targetObject">The entity or model object to set the value on</param>
        /// <param name="propertyInfo">The property info (can be null for ExpandoObject)</param>
        /// <param name="value">The value to set</param>
        /// <param name="propertyName">The property name to use if propertyInfo is null</param>
        public static void SetPropertyValue(object targetObject, PropertyInfo propertyInfo, object value, string propertyName = null)
        {
            if (targetObject == null)
                return;

            if (targetObject is ExpandoObject expandoObj)
            {
                // For ExpandoObject, use dictionary indexer with the property name
                var dictionary = (IDictionary<string, object>)expandoObj;

                // Use either the property info name or the provided property name
                var name = propertyInfo?.Name ?? propertyName;

                if (!string.IsNullOrEmpty(name))
                {
                    dictionary[name] = value;
                }
            }
            else if (propertyInfo != null)
            {
                // For regular objects, use reflection
                propertyInfo.SetValue(targetObject, value);
            }
        }
    }
}
