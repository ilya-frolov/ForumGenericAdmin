using Dino.Common.Helpers;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using Dino.Common.AzureExtensions.Files.Uploaders;
using Dino.Infra.Reflection;
using Dino.CoreMvc.Admin.Models.Exceptions;
using Microsoft.AspNetCore.Http;
using Dino.CoreMvc.Admin.Logic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Dino.Infra.Files.Uploaders;
using Dino.CoreMvc.Admin.Models.Admin;
using Dino.CoreMvc.Admin.Attributes.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Dino.CoreMvc.Admin.Controllers
{
    public abstract partial class DinoAdminBaseEntityController<TModel, TEFEntity, TIdType>
    {
        /// <summary>
        /// Used to avoid mapping of properties that are not existing on the database entity.
        /// </summary>
        public virtual bool ErrorsOnNoneExistingEntityPropertiesMapping => true;

        [HttpGet]
        public virtual async Task<JsonResult> GetFormStructure(string id = null, string refId = null)
        {
            if (!await CheckPermission(PermissionType.View, refId))
            {
                return CreateJsonResponse(false, null, "You do not have permission to view this form.", false);
            }

            try
            {
                // Create or retrieve the model instance FIRST
                dynamic model = null;

                // If ID is provided, load the existing entity and map to model
                TEFEntity entity = null;
                if (!string.IsNullOrEmpty(id))
                {
                    entity = await GetEntityById(id);

                    if (entity != null)
                    {
                        var adminModelType = GetAdminModelType(id, null, entity);

                        model = ModelMappingExtensions.ToAdminModelFromTypes(entity, adminModelType, entity.GetType(), MappingContext).Model;
                        //model = entity.ToAdminModel<TModel, TEFEntity>(MappingContext).Model;
                    }
                    else
                    {
                        return CreateJsonResponse(false, null, $"Entity with ID {id} not found", false);
                    }
                }
                else
                {
                    // Create a new instance with default values
                    model = new TModel();
                    ApplyDefaultValues(model);
                }

                // Create the structure container AFTER we have the model
                var formStructure = new DynamicFormStructure<TModel>
                {
                    ModelType = typeof(TModel).Name,
                    EntityType = typeof(TEFEntity).Name,
                    InputOptions = new Dictionary<string, List<ListDef.SelectOption>>(),
                    Model = model, // Set the model immediately
                    ForeignTypes = new Dictionary<string, FormNodeContainer>() // Add dictionary for foreign types
                };

                // Build the structure and set values in a single pass
                formStructure.Structure = await BuildModelStructureWithValues(model, formStructure.InputOptions,
                    GetAdminModelType(id, model, entity),
                    true, null, null, formStructure.ForeignTypes);

                return CreateJsonResponse(true, formStructure, null, true);
            }
            catch (MissingEndContainerException ex)
            {
                return CreateJsonResponse(false, null, ex.Message, false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating form structure");
                return CreateJsonResponse(false, null, $"Error generating form structure: {ex.Message}", false);
            }
        }

        #region Form Structure Building

        /// <summary>
        /// Recursively builds the model structure with containers, tabs, and fields, and sets values from the model
        /// </summary>
        /// <param name="model">The model instance to get values from</param>
        /// <param name="inputOptions">Dictionary to store input options</param>
        /// <param name="type">The type to build structure for (defaults to TModel)</param>
        /// <param name="createRoot">Whether to create a root container (true for top level, false for nested)</param>
        /// <param name="containerName">Optional name for the container</param>
        /// <param name="properties">Optional properties to use (if null, will get from type)</param>
        /// <param name="foreignTypes">Dictionary to store complex type structures to avoid nesting</param>
        /// <param name="propertyPrefix">Optional prefix for nested properties</param>
        /// <returns>A FormNodeContainer representing the structure</returns>
        protected virtual async Task<FormNodeContainer> BuildModelStructureWithValues(
            object model,
            Dictionary<string, List<ListDef.SelectOption>> inputOptions,
            Type type = null,
            bool createRoot = true,
            string containerName = null,
            IEnumerable<PropertyInfo> properties = null,
            Dictionary<string, FormNodeContainer> foreignTypes = null,
            string propertyPrefix = "")
        {
            // Determine the type to use
            type = type ?? typeof(TModel);

            // Create container
            var container = new FormNodeContainer
            {
                Name = containerName ?? type.Name,
                NodeType = createRoot ? FormNodeType.Root : FormNodeType.SubType,
                Children = new List<FormNode>(),
                Attributes = new Dictionary<string, object>()
            };

            // Maintain a stack for nested containers
            var containerStack = new Stack<FormNodeContainer>();
            containerStack.Push(container);

            // Get properties if not provided
            properties = properties ?? type.GetProperties();

            foreach (var property in properties)
            {
                // Get ALL attributes in declaration order (order of appearance in the code)
                var allAttributes = property.GetCustomAttributes().ToList();

                // Process section attributes (tabs and containers) FIRST
                var sectionAttributes = allAttributes.Where(a => a is SectionAttribute).Cast<SectionAttribute>().ToList();
                foreach (var sectionAttr in sectionAttributes)
                {
                    var nodeTypeString = Regex.Match(sectionAttr.GetType().Name, @"^(\w+)Attribute$").Groups[1].Value;  // Get the type of the section from the attribute name.

                    // Convert string node type to enum
                    FormNodeType nodeType;
                    if (Enum.TryParse(nodeTypeString, out nodeType) == false)
                    {
                        // Default to Container if not recognized
                        nodeType = FormNodeType.Container;
                    }

                    var sectionNode = new FormNodeContainer
                    {
                        Name = sectionAttr.Title,
                        NodeType = nodeType,
                        Children = new List<FormNode>(),
                        Attributes = new Dictionary<string, object>()
                    };

                    // Map section attributes
                    MapAttributeProperties(sectionAttr, sectionNode.Attributes);

                    containerStack.Peek().Children.Add(sectionNode);
                    containerStack.Push(sectionNode);
                }

                // NOW process the field attributes
                var commonAttr = property.GetCustomAttribute<AdminFieldCommonAttribute>();
                var complexAttr = property.GetCustomAttribute<BaseComplexAttribute>();
                if ((commonAttr != null) || (complexAttr != null))
                {
                    // Calculate the current property path for nested fields
                    string currentPropertyPath = string.IsNullOrEmpty(propertyPrefix) ?
                        property.Name : $"{propertyPrefix}.{property.Name}";

                    var fieldNode = await CreateFieldNodeWithValue(property, model, inputOptions, foreignTypes, currentPropertyPath);
                    if (fieldNode != null)
                    {
                        containerStack.Peek().Children.Add(fieldNode);
                    }
                }

                // Process EndSection attributes (EndContainer and EndTab) AFTER adding the field
                // Process ALL end section attributes in the order they appear (order of appearance in the code)
                var endSectionAttributes = allAttributes.Where(a => a is EndSectionAttribute).Cast<EndSectionAttribute>().ToList();
                foreach (var endSectionAttribute in endSectionAttributes)
                {
                    var sectionTypeString = Regex.Match(endSectionAttribute.GetType().Name, @"^End(\w+)Attribute$").Groups[1].Value;  // Get the type of the section from the attribute name.

                    // Convert string section type to enum
                    FormNodeType sectionType;
                    if (Enum.TryParse(sectionTypeString, out sectionType) == false)
                    {
                        // Default to Container if not recognized
                        sectionType = FormNodeType.Container;
                    }

                    if (containerStack.Count <= 1)
                    {
                        throw new MissingEndContainerException($"Too many End{sectionTypeString} attributes found. Check your model definition.");
                    }

                    var poppedSection = containerStack.Pop();

                    if (poppedSection.NodeType != sectionType)
                    {
                        throw new MissingEndContainerException($"End{sectionTypeString} found but the current container is of another type. Check around property: {property.Name}");
                    }
                }
            }

            // Check if we have unclosed containers
            if (containerStack.Count > 1)
            {
                var unclosedContainer = containerStack.Pop();
                throw new MissingEndContainerException($"Missing End{unclosedContainer.NodeType} for {unclosedContainer.NodeType} named '{unclosedContainer.Name}' (Opening attribute type: {unclosedContainer.NodeType})");
            }

            return container;
        }

        /// <summary>
        /// Creates a field node with all its properties and attributes, and sets its value from the model
        /// </summary>
        protected virtual async Task<FormNodeField> CreateFieldNodeWithValue(
            PropertyInfo property,
            object model,
            Dictionary<string, List<ListDef.SelectOption>> inputOptions,
            Dictionary<string, FormNodeContainer> foreignTypes = null,
            string propertyPath = "")
        {
            // Get the AdminFieldCommon attribute or complex attribute (required for all fields).
            var commonAttr = property.GetCustomAttribute<AdminFieldCommonAttribute>(); ;

            if (commonAttr == null)
                return null;

            // Find the field type attribute (deriving from AdminFieldBaseAttribute)
            var fieldTypeAttr = property.GetCustomAttributes()
                .FirstOrDefault(a => a.IsOfTypeOrInherits(typeof(AdminFieldBaseAttribute))) as AdminFieldBaseAttribute;

            var complexAttr = property.GetCustomAttributes().FirstOrDefault(attr =>
                attr.IsOfTypeOrInherits(typeof(BaseComplexAttribute))) as BaseComplexAttribute;

            // Create field node with basic information
            var fieldNode = new FormNodeField
            {
                Name = property.Name,
                DisplayName = commonAttr.Name,
                NodeType = FormNodeType.Field,
                FieldType = fieldTypeAttr?.FieldType,       // This is ONLY the field type, not the complex. A repeater of complex type would be NULL.
                PropertyType = property.PropertyType.Name,
                Attributes = new Dictionary<string, object>(),
            };

            // Map common attributes from AdminFieldCommonAttribute
            MapAttributeProperties(commonAttr, fieldNode.Attributes);

            // Map additional attributes from field type-specific attribute
            if (fieldTypeAttr != null)
            {
                MapAttributeProperties(fieldTypeAttr, fieldNode.Attributes);

                // If this is a select attribute (or a subclass of select attribute, like multi-select), generate an options key and collect options now
                if (fieldTypeAttr.IsOfTypeOrInherits(typeof(AdminFieldSelectAttribute)))
                {
                    AdminFieldSelectAttribute selectAttr = (AdminFieldSelectAttribute)fieldTypeAttr;

                    string optionsKey = $"{selectAttr.SourceType}_{selectAttr.OptionsSource}";
                    fieldNode.InputOptionsKey = optionsKey;

                    // Collect select options inline if they haven't been processed yet
                    if (inputOptions != null && !inputOptions.ContainsKey(optionsKey))
                    {
                        inputOptions[optionsKey] = await GetSelectOptions(selectAttr);
                    }
                }
            }

            // Get the VisibilitySettings attribute
            var visibilityAttr = property.GetCustomAttribute<VisibilitySettingsAttribute>();

            // Map visibility settings attributes
            if (visibilityAttr != null)
            {
                MapAttributeProperties(visibilityAttr, fieldNode.Attributes);
            }

            // Process visibility conditions - use the property path for nested lookups
            fieldNode.VisibilityConditions = GetVisibilityConditions(property, propertyPath);

            // Handle repeater
            if (complexAttr != null)
            {
                // The name of the attribute, without the word "attribute" at the end.
                fieldNode.ComplexType = Regex.Replace(complexAttr.GetType().Name, "Attribute$", "");

                // Map any additional properties from RepeaterAttribute to ComplexTypeSettings
                // that aren't explicitly set above
                // ComplexTypeSettings is not initialized - need to create it first
                fieldNode.ComplexTypeSettings = new Dictionary<string, object>();
                MapAttributeProperties(complexAttr, fieldNode.ComplexTypeSettings);

                // If repeater type is a complex object (not a primitive), build its structure too
                if (!complexAttr.Type.IsPrimitive &&
                    complexAttr.Type != typeof(string) &&
                    complexAttr.Type != typeof(DateTime))
                {
                    // Check if this complex type is already in the foreignTypes dictionary
                    string typeName = complexAttr.Type.Name;

                    if (foreignTypes != null && !foreignTypes.ContainsKey(typeName))
                    {
                        // Add itself, so if the recursion goes down to this type, it will use itself and not create a new instance.
                        foreignTypes[typeName] = new FormNodeContainer();

                        // Build the foreign type structure once - passing the foreignTypes dictionary to capture nested types
                        var structure = await BuildModelStructureWithValues(
                            null,                  // No instance for the template
                            inputOptions,
                            complexAttr.Type,      // The type to build the structure for
                            false,                 // Not a root container
                            complexAttr.Type.Name, // Use type name for container name
                            null,                  // Use all properties
                            foreignTypes,          // Pass the foreignTypes dictionary to capture nested types
                            propertyPath           // Pass the current property path for nested visibility conditions
                        );

                        // Add to foreign types dictionary at the root level
                        foreignTypes[typeName] = structure;
                    }

                    // Store the type name for reference instead of nesting the structure
                    fieldNode.ComplexTypeSettings["typeName"] = typeName;
                }
            }

            return fieldNode;
        }

        /// <summary>
        /// Creates a properly structured visibility condition with the standard format
        /// </summary>
        protected virtual Dictionary<string, object> CreateVisibilityCondition(bool show, string rule, List<Dictionary<string, object>> conditions)
        {
            return new Dictionary<string, object>
            {
                ["show"] = show,
                ["rule"] = rule,
                ["conditions"] = conditions
            };
        }

        /// <summary>
        /// Creates a simple leaf condition for the conditions array
        /// </summary>
        protected virtual Dictionary<string, object> CreateCondition(string property, string op, object value)
        {
            return new Dictionary<string, object>
            {
                ["property"] = property,
                ["operator"] = op,
                ["value"] = value ?? string.Empty
            };
        }

        /// <summary>
        /// Gets visibility conditions for a property
        /// </summary>
        protected virtual List<Dictionary<string, object>> GetVisibilityConditions(PropertyInfo property, string propertyPath = "")
        {
            var visibilityConditions = new List<Dictionary<string, object>>();

            // Process attribute-based conditions
            var showIfAttributes = property.GetCustomAttributes<ShowIfAttribute>();
            var hideIfAttributes = property.GetCustomAttributes<HideIfAttribute>();

            foreach (var attr in showIfAttributes)
            {
                // Use helper method to create consistently formatted condition
                var conditions = new List<Dictionary<string, object>>
                {
                    CreateCondition(attr.PropertyName, "==", attr.Values)
                };

                visibilityConditions.Add(CreateVisibilityCondition(true, "AND", conditions));
            }

            foreach (var attr in hideIfAttributes)
            {
                // Use helper method to create consistently formatted condition
                var conditions = new List<Dictionary<string, object>>
                {
                    CreateCondition(attr.PropertyName, "==", attr.Values)
                };

                visibilityConditions.Add(CreateVisibilityCondition(false, "AND", conditions));
            }

            // Process rule-based conditions if available
            try
            {
                var rules = new ConditionalVisibilityRules<TModel>();
                GetVisibilityRules(rules);

                // Use the full property path for complex types
                string fullPropertyPath = string.IsNullOrEmpty(propertyPath) ? property.Name : propertyPath;

                foreach (var group in rules.GetGroups())
                {
                    var showProperties = group.GetShowProperties();
                    var hideProperties = group.GetHideProperties();

                    // Check for exact match or if we're a child property of a complex type path
                    bool shouldShow = IsPropertyAffectedByVisibilityRule(showProperties, fullPropertyPath);
                    bool shouldHide = IsPropertyAffectedByVisibilityRule(hideProperties, fullPropertyPath);

                    if (shouldShow || shouldHide)
                    {
                        try
                        {
                            // Determine condition type based on show/hide
                            bool showValue = shouldShow;

                            // Get the condition tree from the group
                            var conditionTree = group.GetConditionTree();
                            if (conditionTree != null)
                            {
                                // Create a condition with standard format
                                var condition = CreateVisibilityCondition(showValue, conditionTree.Rule, new List<Dictionary<string, object>>());

                                // Process the condition tree recursively
                                ProcessConditionNode(conditionTree, condition);

                                // Add to visibility conditions
                                visibilityConditions.Add(condition);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, $"Error processing visibility condition for property {property.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error processing visibility rules");
            }

            return visibilityConditions;
        }

        /// <summary>
        /// Check if a property is affected by the visibility rules
        /// </summary>
        private bool IsPropertyAffectedByVisibilityRule(List<string> ruleProperties, string propertyPath)
        {
            if (ruleProperties == null || ruleProperties.Count == 0 || string.IsNullOrEmpty(propertyPath))
                return false;

            // Check if the property is directly in the list
            if (ruleProperties.Contains(propertyPath))
                return true;

            // For complex/nested properties, we check:

            // 1. If it's a direct property of a complex type (e.g., "ItemsExtraData.Name" where propertyPath is "Name" inside ItemsExtraData)
            foreach (var rulePath in ruleProperties)
            {
                if (rulePath.Contains("."))
                {
                    string rulePropertyName = rulePath?.Split('.').Last();
                    string propertyName = propertyPath.Contains(".") ? propertyPath.Split('.').Last() : propertyPath;

                    if (rulePropertyName == propertyName)
                    {
                        // If the full path contains our parent path, or our parent path contains the rule path parent
                        string ruleParentPath = rulePath.Substring(0, rulePath.LastIndexOf('.'));

                        if (propertyPath.Contains("."))
                        {
                            string propertyParentPath = propertyPath.Substring(0, propertyPath.LastIndexOf('.'));
                            if (propertyParentPath.Contains(ruleParentPath) || ruleParentPath.Contains(propertyParentPath))
                                return true;
                        }
                        else if (rulePath == propertyPath)
                        {
                            // For simple property name that matches the end of a complex path
                            return true;
                        }
                    }
                }
            }

            // 2. If we're targeting the complex type itself.
            foreach (var rulePath in ruleProperties)
            {
                if (!string.IsNullOrEmpty(rulePath) &&
                    propertyPath.Contains(".") &&
                    (propertyPath == rulePath))
                    return true;
            }

            return false;
        }

        protected virtual void GetVisibilityRules(ConditionalVisibilityRules<TModel> rules)
        {
        }

        /// <summary>
        /// Maps properties from an attribute to any target object,
        /// skipping properties marked with IgnoreAttributeMapping attribute
        /// </summary>
        protected virtual void MapAttributeProperties(object attribute, object target, string prefix = "")
        {
            if (attribute == null || target == null)
                return;

            // If target is a dictionary, use dictionary mapping
            if (target is IDictionary<string, object> dict)
            {
                MapAttributeProperties(attribute, dict, prefix);
                return;
            }

            // Otherwise, map directly to target object properties
            var attributeType = attribute.GetType();
            var targetType = target.GetType();
            var attributeProps = attributeType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var sourceProp in attributeProps)
            {
                // Skip properties marked with IgnoreAttributeMapping
                if (sourceProp.GetCustomAttribute<IgnoreAttributeListingInResponseAttribute>() != null)
                    continue;

                // Skip common .NET properties that we don't need to map
                if (sourceProp.Name == "TypeId")
                    continue;

                // Try to find a matching property on target
                var value = sourceProp.GetValue(attribute);
                if (value != null)
                {
                    var targetProp = targetType.GetProperty(sourceProp.Name);
                    if (targetProp != null && targetProp.CanWrite)
                    {
                        try
                        {
                            // Try to set the property value using reflection for type conversion
                            if (targetProp.PropertyType != sourceProp.PropertyType)
                            {
                                var converter = TypeDescriptor.GetConverter(targetProp.PropertyType);
                                if (converter.CanConvertFrom(sourceProp.PropertyType))
                                {
                                    value = converter.ConvertFrom(value);
                                }
                            }
                            targetProp.SetValue(target, value);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, $"Failed to map property {sourceProp.Name} to {targetProp.Name}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Maps properties from an attribute to the attributes dictionary, 
        /// skipping properties marked with IgnoreAttributeMapping attribute
        /// </summary>
        protected virtual void MapAttributeProperties(object attribute, Dictionary<string, object> attributesDict, string prefix = "")
        {
            if (attribute == null)
                return;

            var type = attribute.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // Skip properties marked with IgnoreAttributeMapping
                if (prop.GetCustomAttribute<IgnoreAttributeListingInResponseAttribute>() != null)
                    continue;

                // Skip common .NET properties that we don't need to map
                if (prop.Name == "TypeId")
                    continue;

                var value = prop.GetValue(attribute);
                if (value != null)
                {
                    // For enum values, convert to their underlying numeric value for proper serialization
                    if (prop.PropertyType.IsEnum)
                    {
                        value = Convert.ChangeType(value, Enum.GetUnderlyingType(prop.PropertyType));
                    }

                    string key = prefix + prop.Name.ToCamelCase(); // Convert to camelCase for JSON
                    attributesDict[key] = value;
                }
            }
        }

        /// <summary>
        /// Apply default values to a new model instance
        /// </summary>
        protected virtual void ApplyDefaultValues(TModel model)
        {
            var properties = typeof(TModel).GetProperties();
            foreach (var property in properties)
            {
                // Check for DefaultValue attribute
                var commonAttr = property.GetCustomAttribute<AdminFieldCommonAttribute>();
                if (commonAttr?.DefaultValue != null)
                {
                    SetPropertyValue(model, property, commonAttr.DefaultValue);
                }
                else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                {
                    // Default DateTime properties to current date/time
                    SetPropertyValue(model, property, DateTime.Now);
                }
                else if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
                {
                    // Default Guid properties to a new Guid
                    SetPropertyValue(model, property, Guid.NewGuid());
                }
                else if (property.PropertyType.IsGenericType &&
                        (property.PropertyType.GetGenericTypeDefinition() == typeof(List<>) ||
                         property.PropertyType.GetGenericTypeDefinition() == typeof(IList<>) ||
                         property.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>)))
                {
                    // Initialize collection properties to empty collections
                    var listType = typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]);
                    SetPropertyValue(model, property, Activator.CreateInstance(listType));
                }
            }

            // Allow for any custom default values beyond what attributes provide
            ApplyCustomDefaultValues(model);
        }

        /// <summary>
        /// Hook for derived classes to apply custom default values
        /// </summary>
        protected virtual void ApplyCustomDefaultValues(TModel model)
        {
            // Override in derived controllers to apply custom defaults
        }

        /// <summary>
        /// Helper method to safely set property values
        /// </summary>
        protected virtual void SetPropertyValue(object model, PropertyInfo property, object value)
        {
            if (property.CanWrite)
            {
                try
                {
                    // Try to convert the value to the property type
                    if (value != null && property.PropertyType != value.GetType())
                    {
                        if (property.PropertyType.IsEnum)
                        {
                            // Handle enum conversion
                            value = Enum.Parse(property.PropertyType, value.ToString());
                        }
                        else if (property.PropertyType.IsGenericType &&
                                property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            // Handle nullable types
                            var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                            value = Convert.ChangeType(value, underlyingType);
                        }
                        else
                        {
                            // Standard conversion
                            value = Convert.ChangeType(value, property.PropertyType);
                        }
                    }

                    property.SetValue(model, value);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, $"Failed to set default value for property {property.Name}");
                }
            }
        }

        /// <summary>
        /// Get an entity by its ID
        /// </summary>
        protected virtual async Task<TEFEntity?> GetEntityById(string id)
        {
            if (string.IsNullOrEmpty(id) || DbContext == null)
                return null;

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(TIdType));
                var typedId = (TIdType)converter.ConvertFrom(id);

                return await DbContext.Set<TEFEntity>().FindAsync(typedId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error retrieving entity with ID {id}");
                return null;
            }
        }

        /// <summary>
        /// Maps additional properties from source to target, skipping specified properties and those with IgnoreAttributeMapping
        /// </summary>
        protected virtual void MapAdditionalProperties(object source, object target, params string[] propertiesToSkip)
        {
            if (source == null || target == null)
                return;

            var sourceType = source.GetType();
            var targetType = target.GetType();
            var sourceProps = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var skipProps = new HashSet<string>(propertiesToSkip);

            foreach (var sourceProp in sourceProps)
            {
                // Skip properties that are explicitly requested to be skipped
                if (skipProps.Contains(sourceProp.Name))
                    continue;

                // Skip properties marked with IgnoreAttributeMapping
                if (sourceProp.GetCustomAttribute<IgnoreAttributeListingInResponseAttribute>() != null)
                    continue;

                // Skip common .NET properties that we don't need to map
                if (sourceProp.Name == "TypeId")
                    continue;

                // Skip DateType properties as they need special handling
                if ((sourceProp.Name == "ItemType" || sourceProp.Name == "DateType") &&
                    sourceProp.PropertyType == typeof(Type))
                    continue;

                // Try to find a matching property on target
                var targetProp = targetType.GetProperty(sourceProp.Name);
                if (targetProp != null && targetProp.CanWrite)
                {
                    try
                    {
                        var value = sourceProp.GetValue(source);
                        if (value != null)
                        {
                            // Try to set the property value, converting if needed
                            if (targetProp.PropertyType != sourceProp.PropertyType &&
                                value is IConvertible convertible)
                            {
                                value = Convert.ChangeType(value, targetProp.PropertyType);
                            }
                            targetProp.SetValue(target, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, $"Failed to map additional property {sourceProp.Name} to {targetProp.Name}");
                    }
                }
            }
        }

        #endregion

        #region Add

        public virtual async Task<JsonResult> Save(string id)
        {
            bool isNew = string.IsNullOrEmpty(id);
            PermissionType requiredPermission = isNew ? PermissionType.Add : PermissionType.Edit;

            HttpContext.Request.EnableBuffering();
            HttpContext.Request.Body.Position = 0;
            using var reader = new StreamReader(HttpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Parse JSON to get the properties that were actually included in the request
            HashSet<string> includedProperties = null;
            if (!isNew) // Only track for updates, not for new entities
            {
                try
                {
                    var jsonObject = JObject.Parse(body);
                    includedProperties = new HashSet<string>(jsonObject.Properties().Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to parse JSON for property tracking");
                }
            }

            // Deserialize manually.
            var adminType = GetAdminModelType(id, null, null);      // Get actual admin type.
            dynamic model = JsonConvert.DeserializeObject(body, adminType);

            var saveResult = await SaveEntity(id, model, HttpContext, false, includedProperties);
            if (!saveResult.Item1)
            {
                throw new Exception(saveResult.Item2);
            }

            // Call the relevant caching method.
            var finalizedModel = saveResult.Item4;
            var finalId = ModelMappingExtensions.GetEntityId(finalizedModel);
            if (id.IsNullOrEmpty())
            {
                await OnEntityCreatedForCache(model, finalizedModel, finalId);
            }
            else
            {
                await OnEntityUpdatedForCache(model, finalizedModel, finalId);
            }

            return CreateJsonResponse(saveResult.Item1, saveResult.Item3, saveResult.Item2, false);
        }

        #region SaveEntity

        protected virtual async Task<(bool result, string error, dynamic resultModel, TEFEntity efModel)> SaveEntity(string id, TModel model, HttpContext context = null, bool cloneFiles = false, HashSet<string> includedProperties = null)
        {
            var result = false;
            string error = null;
            dynamic resultModel = null;

            if (context == null)
            {
                context = HttpContext;
            }

            var isNew = id.IsNullOrEmpty();

            TEFEntity efModel = null;
            // Create a custom mapping context for this operation
            var mappingContext = MappingContext;
            if (includedProperties != null && includedProperties.Count > 0)
            {
                // Create a new mapping context with the included properties
                mappingContext = new ModelMappingContext
                {
                    PluginRegistry = MappingContext.PluginRegistry,
                    CurrentUserId = MappingContext.CurrentUserId,
                    JsonProcessingDepth = MappingContext.JsonProcessingDepth,
                    IncludedProperties = includedProperties,
                    DbContext = DbContext
                };
            }

            if (isNew)
            {
                // If new
                await RunCustomBeforeMapping(id, model);

                // If it's a new model, we need to set the order-index (if it exists).
                var sortIndexAttribute = typeof(TModel).GetPropertyWithAttribute<SortIndexAttribute>();
                if (sortIndexAttribute != null)
                {
                    // Get the max sort index value using reflection
                    var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TEFEntity), "x");
                    var property = System.Linq.Expressions.Expression.Property(parameter, sortIndexAttribute.Name);
                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<TEFEntity, int>>(property, parameter);

                    int maxSortIndex = DbContext.Set<TEFEntity>().Any() ?
                        DbContext.Set<TEFEntity>().Max(lambda) : 0;
                    ModelMappingExtensions.SetSortIndex(model, maxSortIndex + 1);
                }

                var convertResult = model.ToDbEntity<TModel, TEFEntity>(mappingContext, null);

                if (convertResult.ConvertResult.Success)
                {
                    efModel = convertResult.Entity;

                    DbContext.Add<TEFEntity>(efModel);
                    result = true;
                }
                else
                {
                    // Handle conversion errors
                    error = ConvertValidationErrorsToReadableString(convertResult.ConvertResult);
                }
            }
            else
            {
                // if edit
                efModel = await GetEntityById(id);
                if (efModel != null)
                {
                    await RunCustomBeforeMapping(id, model);
                    var convertResult = model.ToDbEntity<TModel, TEFEntity>(mappingContext, efModel);

                    if (convertResult.ConvertResult.Success)
                    {
                        result = true;
                    }
                    else
                    {
                        // Handle conversion errors
                        error = ConvertValidationErrorsToReadableString(convertResult.ConvertResult);
                    }
                }
            }

            // Submit the changes
            if (result)
            {
                await RunCustomBeforeSave(id, model, efModel);

                await DbContext.SaveChangesAsync();

                await RunCustomAfterSave(id, model, efModel);
            }

            var adminModelType = GetAdminModelType(id, null, null);      // Get actual admin type.

            var conversionResult = ModelMappingExtensions.ToAdminModelFromTypes(efModel, adminModelType, efModel?.GetType(), MappingContext);

            resultModel = conversionResult.Model;
            return (result, error, resultModel, efModel);
        }

        private string ConvertValidationErrorsToReadableString(ModelConvertResult result)
        {
            if (result == null || result.ValidationErrors == null || result.ValidationErrors.Count == 0)
            {
                return string.Empty;
            }

            var errorMessages = new List<string>();

            foreach (var propertyErrors in result.ValidationErrors)
            {
                foreach (var error in propertyErrors.Value)
                {
                    string message = $"Property: {error.PropertyName ?? propertyErrors.Key}";
                    if (!string.IsNullOrEmpty(error.Message))
                    {
                        message += $", \r\nError: {error.Message}";
                    }
                    if (!string.IsNullOrEmpty(error.ErrorCode))
                    {
                        message += $", \r\nCode: {error.ErrorCode}";
                    }
                    errorMessages.Add(message);
                }
            }

            return string.Join(";\r\n\r\n", errorMessages);
        }

        #region RunCustomBeforeMapping

        /// <summary>
        /// Override this method to add custom logic before the model mapping occurs
        /// </summary>
        /// <param name="id">The entity id</param>
        /// <param name="model">The model.</param>
        protected virtual async Task RunCustomBeforeMapping(string id, TModel model)
        {
            // Override this method to add custom logic
        }

        #endregion

        #region RunCustomBeforeSave

        /// <summary>
        /// Override this method to add custom logic before the model save
        /// </summary>
        /// <param name="id">The entity id</param>
        /// <param name="model">The model.</param>
        /// <param name="efModel">The EF model.</param>
        protected virtual async Task RunCustomBeforeSave(string id, TModel model, TEFEntity efModel)
        {
            // Override this method to add custom logic
        }

        #endregion

        #region RunCustomAfterSave

        /// <summary>
        /// Override this method to add custom logic after the model save
        /// </summary>
        /// <param name="id">The entity id</param>
        /// <param name="model">The model.</param>
        /// <param name="efModel">The EF model.</param>
        protected virtual async Task RunCustomAfterSave(string id, TModel model, TEFEntity efModel)
        {
            // Override this method to add custom logic
        }

        #endregion

        #endregion

        #endregion

        // Helper method to process condition nodes recursively
        void ProcessConditionNode(ConditionNode node, Dictionary<string, object> output)
        {
            if (node == null) return;

            if (node.NodeType == ConditionNodeType.Group)
            {
                // For group nodes, set the rule and build the conditions array
                output["rule"] = node.Rule;

                var childConditions = new List<object>();
                foreach (var child in node.Children)
                {
                    if (child.NodeType == ConditionNodeType.Condition)
                    {
                        // For leaf conditions, use helper method
                        childConditions.Add(CreateCondition(child.Property, child.Operator, child.Value));
                    }
                    else if (child.NodeType == ConditionNodeType.Group)
                    {
                        // For nested groups, create a new group and process recursively
                        var nestedConditions = new List<object>();

                        // Create a nested group with the appropriate rule
                        var nestedGroup = new Dictionary<string, object>
                        {
                            ["rule"] = child.Rule,
                            ["conditions"] = nestedConditions
                        };

                        // Process each child in the nested group
                        foreach (var nestedChild in child.Children)
                        {
                            if (nestedChild.NodeType == ConditionNodeType.Condition)
                            {
                                // For leaf conditions, use helper method
                                nestedConditions.Add(CreateCondition(nestedChild.Property, nestedChild.Operator, nestedChild.Value));
                            }
                            else if (nestedChild.NodeType == ConditionNodeType.Group)
                            {
                                // For nested groups, create a container and process recursively
                                var innerGroup = new Dictionary<string, object>();
                                ProcessConditionNode(nestedChild, innerGroup);

                                // Only add if we have properties (this avoids empty containers)
                                if (innerGroup.Count > 0)
                                {
                                    nestedConditions.Add(innerGroup);
                                }
                            }
                        }

                        childConditions.Add(nestedGroup);
                    }
                }

                output["conditions"] = childConditions;
            }
            else if (node.NodeType == ConditionNodeType.Condition)
            {
                // For leaf condition nodes, create a standard condition with AND rule
                output["rule"] = "AND";
                output["conditions"] = new List<object>
                {
                    CreateCondition(node.Property, node.Operator, node.Value)
                };
            }
        }

        /// <summary>
        /// Handles file uploads for file fields
        /// </summary>
        /// <param name="file">The uploaded file</param>
        /// <param name="platform">The platform identifier (1 = Desktop, 2 = Mobile)</param>
        /// <returns>JSON result with the uploaded file path</returns>
        [HttpPost]
        public virtual async Task<JsonResult> UploadFile(IFormFile file)
        {
            if (!await CheckPermission(PermissionType.Add))
            {
                return JsonError("You do not have permission to upload files for this entity.");
            }

            if (file == null || file.Length == 0)
            {
                return JsonError("No file was uploaded");
            }

            try
            {
                // Get file uploader service
                var fileUploader = GetFileUploader();
                if (fileUploader == null)
                {
                    return JsonError("File uploader service is not available");
                }

                // Create a file upload task
                using var stream = file.OpenReadStream();

                //replace whitespace with underscore and add date and time to the file name
                var fileNameWithDate = file.FileName.Replace(" ", "_").Replace(".", "_" + DateTime.UtcNow.Ticks + ".");
                var uploadTask = new FileUploadTask(
                    stream,
                    fileNameWithDate,
                    isCustomPath: false,
                    contentType: file.ContentType
                );

                // Upload the file
                string uploadedPath = await fileUploader.UploadFileAsync(uploadTask);

                return Json(new { result = true, data = uploadedPath });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                return JsonError($"Error uploading file: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to create error JSON response
        /// </summary>
        private JsonResult JsonError(string message)
        {
            return Json(new { result = false, error = message });
        }

        protected IFileUploader GetFileUploader(string containerName = null)
        {
            return BlConfig.Value.StorageConfig.UseAzureBlob ?
                new AzureBlobStorageUploader(BlConfig.Value.StorageConfig.AzureBlobConnectionString,
                    containerName ?? BlConfig.Value.StorageConfig.AzureBlobContainerName, BlConfig.Value.StorageConfig.AzureBlobBaseUrl) :
                new FileSystemFileUploader();
        }
    }
}