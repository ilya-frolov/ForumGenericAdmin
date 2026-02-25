using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Dino.CoreMvc.Admin.Attributes
{
    /// <summary>
    /// Enum for field width
    /// </summary>
    public enum FieldWidth
    {
        /// <summary>
        /// Auto width (default)
        /// </summary>
        Auto = 0,

        /// <summary>
        /// 25% width
        /// </summary>
        Quarter = 25,

        /// <summary>
        /// 33% width
        /// </summary>
        Third = 33,

        /// <summary>
        /// 50% width
        /// </summary>
        Half = 50,

        /// <summary>
        /// 66% width
        /// </summary>
        TwoThirds = 66,

        /// <summary>
        /// 75% width
        /// </summary>
        ThreeQuarters = 75,

        /// <summary>
        /// 100% width
        /// </summary>
        Full = 100
    }

    /// <summary>
    /// Base attribute class for common field properties in admin interfaces.
    /// Provides core functionality like naming, tooltips, visibility, and layout options.
    /// </summary>
    /// <typeparam name="T">The type of the default value for the field</typeparam>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldCommonAttribute : Attribute
    {
        [JsonIgnore]
        public int LineNumber { get; private set; }
        public string Name { get; set; }
        public string Tooltip { get; set; }
        public bool Required { get; set; }
        public bool ReadOnly { get; set; }
        public bool Visible { get; set; }
        public bool Searchable { get; set; }
        public object DefaultValue { get; set; }
        public FieldWidth Width { get; set; }

        /// <summary>
        /// Basic class for field type attributes.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="tooltip">Displays a tooltip on hover</param>
        /// <param name="required">Marks the field as mandatory.</param>
        /// <param name="readOnly">Makes the field non-editable</param>
        /// <param name="visible">Determines if the field should be displayed</param>
        /// <param name="searchable">Determines if the field should be searchable in the ajax filtering.</param>
        /// <param name="defaultValue">Default value for the property (generic)</param>
        /// <param name="width">The width of the field inside the editing page</param>
        /// <param name="lineNumber">DON'T SET! Set automatically!</param>
        public AdminFieldCommonAttribute(string name,
            string tooltip = null,
            bool required = false,
            bool readOnly = false,
            bool visible = true,
            bool searchable = true,
            object defaultValue = null,
            FieldWidth width = FieldWidth.Full,
            [CallerLineNumber] int lineNumber = 0)
        {
            Name = name;
            Tooltip = tooltip;
            Required = required;
            ReadOnly = readOnly;
            Visible = visible;
            Searchable = searchable;
            DefaultValue = defaultValue;
            Width = width;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public abstract class AdminFieldBaseAttribute : Attribute
    {
        [IgnoreAttributeListingInResponse]
        public string FieldType { get; private set; }

        protected AdminFieldBaseAttribute()
        {
            // Automatically derive the field type from the attribute class name
            string typeName = GetType().Name
                .Replace("AdminField", "")
                .Replace("Attribute", "");
            FieldType = typeName;
        }
    }

    /// <summary>
    /// Attribute for configuring text input fields in admin interfaces.
    /// Supports features like placeholders, length limits, and text masking.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldTextAttribute : AdminFieldBaseAttribute
    {
        public string Placeholder { get; set; }
        public int? MaxLength { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string Masking { get; set; }

        /// <summary>
        /// Defines a text input field.
        /// </summary>
        /// <param name="placeholder">Placeholder text displayed inside the field.</param>
        /// <param name="maxLength">Maximum number of characters allowed. -1 = Unlimited.</param>
        /// <param name="prefix">Prefix text before the input.</param>
        /// <param name="suffix">Suffix text after the input.</param>
        /// <param name="masking">Input masking pattern (e.g., phone number).</param>
        public AdminFieldTextAttribute(string placeholder = null, int maxLength = -1, string prefix = null, string suffix = null, string masking = null)
            : base()
        {
            Placeholder = placeholder;
            MaxLength = (maxLength == -1) ? null : maxLength;
            Prefix = prefix;
            Suffix = suffix;
            Masking = masking;
        }
    }

    /// <summary>
    /// Attribute for configuring multi-line text area inputs in admin interfaces.
    /// Extends text input functionality with support for multiple rows.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldTextAreaAttribute : AdminFieldTextAttribute
    {
        public int Rows { get; set; }

        /// <summary>
        /// Defines a text area input field.
        /// </summary>
        /// <param name="placeholder">Placeholder text displayed inside the field.</param>
        /// <param name="maxLength">Maximum number of characters allowed. -1 = Unlimited.</param>
        /// <param name="prefix">Prefix text before the input.</param>
        /// <param name="suffix">Suffix text after the input.</param>
        /// <param name="masking">Input masking pattern (e.g., phone number).</param>
        /// <param name="rows">Number of rows for multi-line text.</param>
        public AdminFieldTextAreaAttribute(string placeholder = null, int maxLength = -1, string prefix = null, string suffix = null, string masking = null, int rows = 3)
            : base(placeholder, maxLength, prefix, suffix, masking)
        {
            Rows = rows;
        }
    }

    public enum RichTextEditorType
    {
        Quill,
        CKEditor
    }

    /// <summary>
    /// Attribute for configuring rich text editor fields in admin interfaces.
    /// Supports different editor types like Quill and CKEditor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldRichTextAttribute : AdminFieldTextAttribute
    {
        public RichTextEditorType EditorType { get; set; }

        /// <summary>
        /// Defines a rich text editor field.
        /// </summary>
        /// <param name="placeholder">Placeholder text displayed inside the field.</param>
        /// <param name="maxLength">Maximum number of characters allowed. -1 = Unlimited.</param>
        /// <param name="prefix">Prefix text before the input.</param>
        /// <param name="suffix">Suffix text after the input.</param>
        /// <param name="masking">Input masking pattern (e.g., phone number).</param>
        /// <param name="editorType">DateType of editor used.</param>
        public AdminFieldRichTextAttribute(string placeholder = null, int maxLength = -1, string prefix = null, string suffix = null, string masking = null, RichTextEditorType editorType = RichTextEditorType.Quill)
            : base(placeholder, maxLength, prefix, suffix, masking)
        {
            EditorType = editorType;
        }
    }

    /// <summary>
    /// Attribute for configuring numeric input fields in admin interfaces.
    /// Supports both integer and decimal values with formatting options.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldNumberAttribute : AdminFieldBaseAttribute
    {
        public bool IsDecimal { get; set; }
        public bool ShowThousandsComma { get; set; }
        public int DecimalPlaces { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Step { get; set; }

        /// <summary>
        /// Defines a number input field.
        /// </summary>
        /// <param name="isDecimal">Whether decimal values are allowed.</param>
        /// <param name="showThousandsComma">Whether to show comma separators for thousands.</param>
        /// <param name="decimalPlaces">Number of decimal places to validate. -1 = No validation.</param>
        /// <param name="min">Minimum value allowed. If null, no minimum validation is performed.</param>
        /// <param name="max">Maximum value allowed. If null, no maximum validation is performed.</param>
        /// <param name="step">Increment step for the input.</param>
        public AdminFieldNumberAttribute(bool isDecimal = false, bool showThousandsComma = true, int decimalPlaces = -1, double min = double.MinValue, double max = double.MaxValue, double step = 1)
            : base()
        {
            IsDecimal = isDecimal;
            ShowThousandsComma = showThousandsComma;
            DecimalPlaces = decimalPlaces;
            Min = min;
            Max = max;
            Step = step;
        }
    }

    /// <summary>
    /// Attribute for configuring checkbox fields in admin interfaces.
    /// Supports optional list-view toggle functionality.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldCheckboxAttribute : AdminFieldBaseAttribute
    {
        public bool AllowListToggle { get; set; }

        /// <summary>
        /// Defines a checkbox input field.
        /// </summary>
        /// <param name="allowListToggle">Do we want to show a toggle in the table itself (that will edit the boolean on click)</param>
        public AdminFieldCheckboxAttribute(bool allowListToggle = false)
            : base()
        {
            AllowListToggle = allowListToggle;
        }
    }

    public enum SelectSourceType
    {
        DbType,
        Function,
        Enum
    }

    public enum SelectViewType
    {
        Dropdown,
        RadioButtons,
        SearchableDropdown,
        Tags,
        Checkboxes
    }

    /// <summary>
    /// Attribute for configuring select/dropdown fields in admin interfaces.
    /// Supports various display types and selection modes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldSelectAttribute : AdminFieldBaseAttribute
    {
        [IgnoreAttributeListingInResponse]
        public SelectSourceType SourceType { get; set; }
        [IgnoreAttributeListingInResponse]
        public object OptionsSource { get; set; }
        [IgnoreAttributeListingInResponse]
        public string NameFieldOrMethod { get; set; }
        [IgnoreAttributeListingInResponse]  
        public string ValueFieldOrMethod { get; set; }
        public bool SearchEnabled { get; set; }
        public SelectViewType ViewType { get; set; }

        /// <summary>
        /// Defines a select input field.
        /// </summary>
        /// <param name="sourceType">The source type for the select.</param>
        /// <param name="optionsSource">Source for options, either a DateType (For DbType and Enum) or method name (Use nameof()).</param>
        /// <param name="nameFieldOrMethod">In case the source is options we need to specify the field or method to for the name parsing. Use nameof()</param>
        /// <param name="valueFieldOrMethod">In case the source is options we need to specify the field or method to for the value parsing. Use nameof()</param>
        /// <param name="multiSelect">Whether multiple selections are allowed.</param>
        /// <param name="searchEnabled">Enables search within the dropdown.</param>
        /// <param name="viewType">Defines UI</param>
        public AdminFieldSelectAttribute(SelectSourceType sourceType = SelectSourceType.DbType, object optionsSource = null, string nameFieldOrMethod = null, string valueFieldOrMethod = null,
            bool searchEnabled = false, SelectViewType viewType = SelectViewType.Dropdown) : base()
        {
            SourceType = sourceType;
            OptionsSource = optionsSource;
            NameFieldOrMethod = nameFieldOrMethod;
            ValueFieldOrMethod = valueFieldOrMethod;
            SearchEnabled = searchEnabled;
            ViewType = viewType;
        }
    }

    /// <summary>
    /// Attribute for configuring multi-select fields in admin interfaces.
    /// Supports both JSON storage and Entity Framework relationships.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldMultiSelectAttribute : AdminFieldSelectAttribute
    {
        /// <summary>
        /// Gets or sets whether the multi-select values should be stored as JSON.
        /// If false, it will be treated as an EF relationship.
        /// </summary>
        public bool StoreAsJson { get; set; }

        /// <summary>
        /// Gets or sets the name of the related collection property in the EF model.
        /// Only used when StoreAsJson is false.
        /// </summary>
        public string RelatedCollectionPropertyName { get; set; }

        /// <summary>
        /// Gets or sets the name of the ID property in the related entity.
        /// Only used when StoreAsJson is false.
        /// </summary>
        public string RelatedEntityIdProperty { get; set; }

        /// <summary>
        /// Defines a multi-select input field.
        /// </summary>
        /// <param name="sourceType">The source type for the select.</param>
        /// <param name="optionsSource">Source for options, either a DateType (For DbType and Enum) or method name (Use nameof()).</param>
        /// <param name="nameFieldOrMethod">In case the source is options we need to specify the field or method for the name parsing. Use nameof()</param>
        /// <param name="valueFieldOrMethod">In case the source is options we need to specify the field or method for the value parsing. Use nameof()</param>
        /// <param name="storeAsJson">Whether to store the values as JSON (true) or as EF relationships (false).</param>
        /// <param name="relatedCollectionPropertyName">The name of the related collection property in the EF model (use nameof()). Only needed when storeAsJson is false.</param>
        /// <param name="relatedEntityIdProperty">The name of the ID property in the related entity (use nameof()). Only needed when storeAsJson is false.</param>
        /// <param name="searchEnabled">Enables search within the dropdown.</param>
        /// <param name="viewType">Defines UI</param>
        public AdminFieldMultiSelectAttribute(
            SelectSourceType sourceType = SelectSourceType.DbType, 
            object optionsSource = null, 
            string nameFieldOrMethod = null,
            string valueFieldOrMethod = null, 
            bool storeAsJson = false,
            string relatedCollectionPropertyName = null,
            string relatedEntityIdProperty = "Id",
            bool searchEnabled = false, 
            SelectViewType viewType = SelectViewType.Tags)
            : base(sourceType, optionsSource, nameFieldOrMethod, valueFieldOrMethod, searchEnabled, viewType)
        {
            StoreAsJson = storeAsJson;
            RelatedCollectionPropertyName = relatedCollectionPropertyName;
            RelatedEntityIdProperty = relatedEntityIdProperty;
        }
    }

    public enum DateTimePickerType
    {
        Date = 1,
        Time = 2,
        DateTime = 3
    }

    /// <summary>
    /// Attribute for configuring date and time input fields in admin interfaces.
    /// Supports different datetime formats and range selection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldDateTimeAttribute : AdminFieldBaseAttribute
    {
        public DateTimePickerType DateType { get; set; }
        public bool IsRangeSelection { get; set; }
        public bool IsUtc { get; set; }

        /// <summary>
        /// Defines a DateTime input field.
        /// </summary>
        /// <param name="dateType">Defines dateType: Date, Time, DateTime.</param>
        /// <param name="isRangeSelection">Whether to allow selection of a date range.</param>
        public AdminFieldDateTimeAttribute(DateTimePickerType dateType = DateTimePickerType.DateTime, bool isRangeSelection = false, bool isUtc = false)
            : base()
        {
            DateType = dateType;
            IsRangeSelection = isRangeSelection;
            IsUtc = isUtc;
        }
    }

    /// <summary>
    /// Attribute for configuring file upload fields in admin interfaces.
    /// Supports file type restrictions, size limits, and platform-specific uploads.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldFileAttribute : AdminFieldBaseAttribute
    {
        public string[] AllowedExtensions { get; set; }
        public double MaxSize { get; set; }
        public bool Multiple { get; set; }
        public bool DragDrop { get; set; }
        public Platforms Platforms { get; set; }

        /// <summary>
        /// Defines a file upload input field.
        /// </summary>
        /// <param name="allowedExtensions">List of permitted file extensions.</param>
        /// <param name="maxSize">Maximum file size in MB.</param>
        /// <param name="multiple">Whether multiple file uploads are allowed.</param>
        /// <param name="dragDrop">Enables drag-and-drop file uploads.</param>
        /// <param name="platforms">Platforms</param>
        public AdminFieldFileAttribute(string[] allowedExtensions = null, double maxSize = 10, bool multiple = false,
            bool dragDrop = true, Platforms platforms = Platforms.Desktop | Platforms.Mobile)
            : base()
        {
            AllowedExtensions = allowedExtensions;
            MaxSize = maxSize;
            Multiple = multiple;
            DragDrop = dragDrop;
            Platforms = platforms;
        }
    }

    public enum ForcePictureFormat
    {
        JPG,
        PNG,
        WebP,
        NO_FORCE = -1
    }

    /// <summary>
    /// Attribute for configuring image upload fields in admin interfaces.
    /// Extends file upload functionality with image-specific features like cropping and format conversion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldPictureAttribute : AdminFieldFileAttribute
    {
        public ForcePictureFormat ForceFormat { get; set; }
        public bool ForceCrop { get; set; }
        public string CropRatio { get; set; }
        public string ResolutionHint { get; set; }

        /// <summary>
        /// Defines a picture upload input field.
        /// </summary>
        /// <param name="allowedExtensions">List of permitted file extensions. If null, will use default picture formats.</param>
        /// <param name="forceFormat">Enforces a specific image format.</param>
        /// <param name="maxSize">Maximum file size in MB.</param>
        /// <param name="multiple">Whether multiple file uploads are allowed.</param>
        /// <param name="dragDrop">Enables drag-and-drop file uploads.</param>
        /// <param name="platforms">Platforms</param>
        /// <param name="forceCrop">Forces cropping before upload.</param>
        /// <param name="cropRatio">Defines aspect ratio for cropping (e.g., 16:9).</param>
        /// <param name="resolutionHint">Recommended resolution (e.g., 1920x1080).</param>
        public AdminFieldPictureAttribute(
            string[] allowedExtensions = null,
            ForcePictureFormat forceFormat = ForcePictureFormat.WebP, double maxSize = 2,
            bool multiple = false, bool dragDrop = true, 
            Platforms platforms = Platforms.Desktop | Platforms.Mobile, bool forceCrop = false,
            string cropRatio = null, string resolutionHint = null)
            : base(allowedExtensions, maxSize, multiple, dragDrop, platforms)
        {
            ForceFormat = forceFormat;
            ForceCrop = forceCrop;
            CropRatio = cropRatio;
            ResolutionHint = resolutionHint;

            if (allowedExtensions == null)
            {
                AllowedExtensions = new[] { "png", "jpg", "jpeg", "webp", "gif" };
            }
        }
    }

    /// <summary>
    /// Attribute for configuring color picker fields in admin interfaces.
    /// Supports alpha channel for transparency.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldColorPickerAttribute : AdminFieldBaseAttribute
    {
        public bool AllowAlpha { get; set; }

        /// <summary>
        /// Defines a color picker input field.
        /// </summary>
        /// <param name="allowAlpha">Whether to allow transparency.</param>
        public AdminFieldColorPickerAttribute(bool allowAlpha = false)
            : base()
        {
            AllowAlpha = allowAlpha;
        }
    }

    public enum CoordinatePickerMapProvider
    {
        Google,
        OpenStreetMap
    }

    /// <summary>
    /// Attribute for configuring map-based coordinate picker fields in admin interfaces.
    /// Supports different map providers and location search functionality.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldCoordinatePickerAttribute : AdminFieldBaseAttribute
    {
        public CoordinatePickerMapProvider MapProvider { get; set; }
        public string DefaultLocation { get; set; }
        public bool AllowSearch { get; set; }

        /// <summary>
        /// Defines a coordinate picker input field.
        /// </summary>
        /// <param name="mapProvider">Defines map source</param>
        /// <param name="defaultLocation">Predefined latitude/longitude.</param>
        /// <param name="allowSearch">Whether location search is enabled.</param>
        public AdminFieldCoordinatePickerAttribute(CoordinatePickerMapProvider mapProvider = CoordinatePickerMapProvider.Google,
            string defaultLocation = null, bool allowSearch = true)
            : base()
        {
            MapProvider = mapProvider;
            DefaultLocation = defaultLocation;
            AllowSearch = allowSearch;
        }
    }

    public enum PasswordStrengthCheck
    {
        None,
        Weak,
        Medium,
        High,
        VeryHigh
    }

    /// <summary>
    /// Attribute for configuring password input fields in admin interfaces.
    /// Supports password strength validation and display options.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldPasswordAttribute : AdminFieldBaseAttribute
    {
        public int MinLength { get; set; }
        public PasswordStrengthCheck StrengthCheck { get; set; }
        public bool ShowAsterisks { get; set; }

        /// <summary>
        /// Defines a password input field.
        /// </summary>
        /// <param name="minLength">Minimum required length.</param>
        /// <param name="strengthCheck">Strength enforcement</param>
        /// <param name="showAsterisks">Whether the password should be hidden with asterisks.</param>
        public AdminFieldPasswordAttribute(int minLength = 8, PasswordStrengthCheck strengthCheck = PasswordStrengthCheck.Medium,
            bool showAsterisks = true)
            : base()
        {
            MinLength = minLength;
            StrengthCheck = strengthCheck;
            ShowAsterisks = showAsterisks;
        }
    }

    public enum ExternalVideoSourceType
    {
        YouTube,
        Vimeo,
        AutoDetect
    }

    /// <summary>
    /// Attribute for configuring external video embed fields in admin interfaces.
    /// Supports different video platforms like YouTube and Vimeo.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldExternalVideoAttribute : AdminFieldBaseAttribute
    {
        public ExternalVideoSourceType SourceType { get; set; }

        /// <summary>
        /// Defines an external video input field.
        /// </summary>
        /// <param name="sourceType">Video platform: YouTube, Vimeo.</param>
        public AdminFieldExternalVideoAttribute(ExternalVideoSourceType sourceType = ExternalVideoSourceType.YouTube)
            : base()
        {
            SourceType = sourceType;
        }
    }

    /// <summary>
    /// Attribute for configuring URL input fields in admin interfaces.
    /// Supports URL validation and link behavior configuration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminFieldURLAttribute : AdminFieldTextAttribute
    {
        public string AlternativeDisplayName { get; set; }
        public string BaseUrl { get; set; }

        /// <summary>
        /// Defines a URL input field.
        /// </summary>
        /// <param name="alternativeDisplayName">The display name of the URL.</param>
        public AdminFieldURLAttribute(string alternativeDisplayName = null, string placeholder = null, int maxLength = -1, string prefix = null, string suffix = null, string masking = null)
            : base(placeholder, maxLength, prefix, suffix, masking)
        {
            AlternativeDisplayName = alternativeDisplayName;
        }
    }

    /// <summary>
    /// Attribute for skipping export.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AdminSkipExportAttribute : AdminFieldBaseAttribute
    {
        public AdminSkipExportAttribute()
        {
        }
    }
}
