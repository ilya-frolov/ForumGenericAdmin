namespace Dino.CoreMvc.Admin.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class BaseComplexAttribute : Attribute
    {
        [IgnoreAttributeListingInResponse]
        public Type Type { get; set; }
        [IgnoreAttributeListingInResponse]
        public bool StoreAsJson { get; set; }
        [IgnoreAttributeListingInResponse]
        public Type RelatedEntity { get; set; }
        public ContainerBehavior ContainerBehavior { get; set; }
        public bool DefaultCollapsed { get; set; }
        public bool ShowTitle { get; set; }
        public string TitleText { get; set; }
        [IgnoreAttributeListingInResponse]
        public bool DeleteOnRemove { get; set; }
        [IgnoreAttributeListingInResponse]
        public bool CascadeDelete { get; set; }

        /// <summary>
        /// Defines the type and behavior of a nested object or repeater item.
        /// </summary>
        /// <param name="type">Defines the type of the nested object. Admin model type.</param>
        /// <param name="storeAsJson">Whether to store this as a JSON string in the database.</param>
        /// <param name="relatedEntity">If set, the repeater represents a relation to another entity (that's INSTEAD of the JSON). The DB type.</param>
        /// <param name="containerBehavior">Defines UI container behavior (None, Collapsible, Tab).</param>
        /// <param name="defaultCollapsed">If containerBehavior is Collapsible, determines default state.</param>
        /// <param name="showTitle">Whether to display a title for the container.</param>
        /// <param name="titleText">Title text for the container.</param>
        /// <param name="deleteOnRemove">Whether to delete related entities when removed from collection.</param>
        /// <param name="cascadeDelete">Whether to cascade delete related entities and their relationships.</param>
        public BaseComplexAttribute(Type type = null, bool storeAsJson = false, Type relatedEntity = null, 
            ContainerBehavior containerBehavior = ContainerBehavior.None, bool defaultCollapsed = false, 
            bool showTitle = true, string titleText = null, bool deleteOnRemove = false, bool cascadeDelete = false)
        : base()
        {
            Type = type;
            StoreAsJson = storeAsJson;
            RelatedEntity = relatedEntity;
            ContainerBehavior = containerBehavior;
            DefaultCollapsed = defaultCollapsed;
            ShowTitle = showTitle;
            TitleText = titleText;
            DeleteOnRemove = deleteOnRemove;
            CascadeDelete = cascadeDelete;
        }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class ComplexTypeAttribute : BaseComplexAttribute
    {
        /// <summary>
        /// Defines a complex type, extending BaseComplex.
        /// </summary>
        /// <param name="type">Defines the type of the nested object.</param>
        /// <param name="storeAsJson">Whether to store this as a JSON string in the database.</param>
        /// <param name="relatedEntity">If set, the complex type represents a relation to another entity (that's INSTEAD of the JSON).</param>
        /// <param name="containerBehavior">Defines UI container behavior (None, Collapsible, Tab).</param>
        /// <param name="defaultCollapsed">If containerBehavior is Collapsible, determines default state.</param>
        /// <param name="showTitle">Whether to display a title for the container.</param>
        /// <param name="titleText">Title text for the container.</param>
        public ComplexTypeAttribute(Type type = null, bool storeAsJson = false, Type relatedEntity = null, 
            ContainerBehavior containerBehavior = ContainerBehavior.None, bool defaultCollapsed = false, 
            bool showTitle = true, string titleText = null) 
            : base(type, storeAsJson, relatedEntity, containerBehavior, defaultCollapsed, showTitle, titleText)
        {
        }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class RepeaterAttribute : BaseComplexAttribute
    {
        public int MinItems { get; set; }
        public int MaxItems { get; set; }
        public bool AllowReordering { get; set; }
        public bool DisableRepeaterAddItemButton { get; set; }
        public bool DisableRepeaterRemoveItemButton { get; set; }
        public bool RepeaterRemoveConfirmation { get; set; }

        /// <summary>
        /// Defines a repeater, extending BaseComplex.
        /// </summary>
        /// <param name="type">Defines the type of the nested object.</param>
        /// <param name="storeAsJson">Whether to store this as a JSON string in the database.</param>
        /// <param name="relatedEntity">If set, the repeater represents a relation to another entity (that's INSTEAD of the JSON).</param>
        /// <param name="containerBehavior">Defines UI container behavior (None, Collapsible, Tab).</param>
        /// <param name="defaultCollapsed">If containerBehavior is Collapsible, determines default state.</param>
        /// <param name="showTitle">Whether to display a title for the container.</param>
        /// <param name="titleText">Title text for the container.</param>
        /// <param name="minItems">Minimum number of items required.</param>
        /// <param name="maxItems">Maximum number of items allowed.</param>
        /// <param name="allowReordering">Whether items in the list can be reordered.</param>
        /// <param name="disableRepeaterAddItemButton">Whether the add item button should be disabled.</param>
        /// <param name="disableRepeaterRemoveItemButton">Whether the remove item button should be disabled.</param>
        /// <param name="repeaterRemoveConfirmation">Whether to require confirmation before removing an item.</param>
        public RepeaterAttribute(Type type = null, bool storeAsJson = false, Type relatedEntity = null, ContainerBehavior containerBehavior = ContainerBehavior.None, bool defaultCollapsed = false, bool showTitle = true,
            string titleText = null, int minItems = 0, int maxItems = 99999, bool allowReordering = true, bool disableRepeaterAddItemButton = false, bool disableRepeaterRemoveItemButton = false,
            bool repeaterRemoveConfirmation = true) : base(type, storeAsJson, relatedEntity, containerBehavior, defaultCollapsed, showTitle, titleText)
        {
            MinItems = minItems;
            MaxItems = maxItems;
            AllowReordering = allowReordering;
            DisableRepeaterAddItemButton = disableRepeaterAddItemButton;
            DisableRepeaterRemoveItemButton = disableRepeaterRemoveItemButton;
            RepeaterRemoveConfirmation = repeaterRemoveConfirmation;
        }
    }
}
