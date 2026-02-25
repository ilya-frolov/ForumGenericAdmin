using System;
using System.Collections.Generic;
using Dino.CoreMvc.Admin.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dino.CoreMvc.Admin.Models.Admin
{
    /// <summary>
    /// Node type enum for form structure nodes
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FormNodeType
    {
        Root = 1,
        Container = 2,
        Tab = 3,
        Field = 4,
        SubType = 5
    }

    /// <summary>
    /// Main container for dynamic form structure and data
    /// </summary>
    public class DynamicFormStructure<FormModel> where FormModel : class
    {
        /// <summary>
        /// The name of the model type
        /// </summary>
        [JsonIgnore]
        public string ModelType { get; set; }

        /// <summary>
        /// The name of the entity type
        /// </summary>
        [JsonIgnore]
        public string EntityType { get; set; }

        /// <summary>
        /// The model structure with all fields, containers, and tabs
        /// </summary>
        public FormNodeContainer Structure { get; set; }

        /// <summary>
        /// Dictionary of select input options
        /// </summary>
        public Dictionary<string, List<ListDef.SelectOption>> InputOptions { get; set; }

        /// <summary>
        /// Dictionary of foreign complex type structures
        /// </summary>
        public Dictionary<string, FormNodeContainer> ForeignTypes { get; set; }

        /// <summary>
        /// The model data
        /// </summary>
        public FormModel Model { get; set; }
    }

    /// <summary>
    /// Base class for all form structure nodes
    /// </summary>
    public abstract class FormNode
    {
        /// <summary>
        /// The name of the node
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the node (Field, Container, Tab, Root)
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public FormNodeType NodeType { get; set; }
    }

    /// <summary>
    /// Container node that can hold other nodes
    /// </summary>
    public class FormNodeContainer : FormNode
    {
        /// <summary>
        /// Attributes for the container (displayMode, etc.)
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; }

        /// <summary>
        /// Children nodes
        /// </summary>
        public List<FormNode> Children { get; set; }
    }

    /// <summary>
    /// Field node representing a form field
    /// </summary>
    public class FormNodeField : FormNode
    {
        /// <summary>
        /// Display name of the field
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// DateType of the field (Text, Number, Select, etc.)
        /// </summary>
        public string FieldType { get; set; }

        /// <summary>
        /// DateType of the property (string, int, etc.)
        /// </summary>
        [JsonIgnore]
        public string PropertyType { get; set; }

        /// <summary>
        /// Field attributes
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; }

        /// <summary>
        /// Visibility conditions for the field
        /// </summary>
        public List<Dictionary<string, object>> VisibilityConditions { get; set; }

        /// <summary>
        /// Key for input options (for select fields)
        /// </summary>
        public string InputOptionsKey { get; set; }

        /// <summary>
        /// Current value of the field
        ///      // NOTE: We decided to remove it. We're getting the values from the Model that is inside the structure.
        /// </summary>
        //public object Value { get; set; }

        /// <summary>
        /// What type of complex is this field, if it is a complex type.
        /// </summary>
        public string ComplexType { get; set; }

        /// <summary>
        /// Repeater settings
        /// </summary>
        public Dictionary<string, object> ComplexTypeSettings { get; set; }
    }
} 