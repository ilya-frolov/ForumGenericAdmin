using Dino.CoreMvc.Admin.Attributes;
using System.Linq.Expressions;
using System.Reflection;
using Dino.Common;
using Dino.Common.Helpers;
using Dino.CoreMvc.Admin.Models.Admin;
using Dino.Infra.Reflection;
using Newtonsoft.Json;

namespace Dino.CoreMvc.Admin.Models
{
    public class ListColumnInfo
    {
        public int PropertyOrderIndex { get; set; }          // Used to be "line number".

        /// <summary>
        /// The type of the property (text, textarea, checkbox, etc).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The key to use when looking up input options in the ListDef.InputOptions dictionary.
        /// Only populated for properties that have select-type inputs.
        /// </summary>
        public string InputOptionsKey { get; set; }

        /// <summary>
        /// The visual name of the property. TODO: Needs to be nullable and retreived from the base edit attribute maybe?
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The property name in the model.
        /// </summary>
        public string PropertyName { get; protected set; }
        public bool AllowSort { get; set; }
        public ColumnFilterType ColumnFilter { get; set; }          // TODO: Reconsider.
        public MainFilterType MainFilter { get; set; }              // TODO: Reconsider.
        public bool FixedColumn { get; set; } = false;
        public bool InlineEdit { get; set; } = false;
        public bool IsSortIndex { get; set; } = false;
        public ReservedAdminPropertiesType? ReservedProperty { get; set; } = null;
    }

    public class ListColumnInfo<T> : ListColumnInfo
    {
        public ListColumnInfo(Expression<Func<T, object>> propertyLambda, PropertyInfo[] properties = null)
        {
            var propertyInfo = ReflectionHelpers.GetMemberInfo(propertyLambda);
            Populate(propertyInfo, properties);
        }

        public ListColumnInfo(MemberInfo propertyInfo, PropertyInfo[] properties = null)
        {
            Populate(propertyInfo, properties);
        }

        private void Populate(MemberInfo propertyInfo, PropertyInfo[] properties = null)
        {
            Name = propertyInfo.Name;
            PropertyName = propertyInfo.Name.ToCamelCase();

            var adminFieldCommonData = propertyInfo.GetCustomAttribute<AdminFieldCommonAttribute>();
            if (adminFieldCommonData == null)
            {
                throw new Exception($"Property {propertyInfo.Name} is missing the AdminFieldCommonAttribute");
            }

            var adminFieldBaseData = propertyInfo.GetCustomAttribute<AdminFieldBaseAttribute>();
            if (adminFieldBaseData == null)
            {
                throw new Exception($"Property {propertyInfo.Name} is missing the AdminFieldBaseAttribute");
            }

            var listSettings = propertyInfo.GetCustomAttribute<ListSettingsAttribute>();
            if (listSettings != null)
            {
                if (adminFieldCommonData.Name.IsNotNullOrEmpty())
                {
                    Name = adminFieldCommonData.Name;
                }

                // Get all properties if it wasn't provided.
                properties ??= typeof(T).GetProperties();

                PropertyOrderIndex = Array.FindIndex(properties, p => p.Name == propertyInfo.Name);
                Type = adminFieldBaseData.FieldType;
                AllowSort = listSettings.AllowSort;
                ColumnFilter = listSettings.ColumnFilter;
                MainFilter = listSettings.MainFilter;
                FixedColumn = listSettings.FixedColumn;
                InlineEdit = listSettings.InlineEdit;
            }

            // Check for special admin property attributes
            if (propertyInfo.GetCustomAttribute<ArchiveIndicatorAttribute>() != null)
            {
                ReservedProperty = ReservedAdminPropertiesType.Archive;
            }
            else if (propertyInfo.GetCustomAttribute<DeletionIndicatorAttribute>() != null)
            {
                ReservedProperty = ReservedAdminPropertiesType.Delete;
            }
            else if (propertyInfo.GetCustomAttribute<SortIndexAttribute>() != null)
            {
                ReservedProperty = ReservedAdminPropertiesType.Sort;
            }
        }
    }
    
    public class ListDef
    {
        public ListDef()
        {
            AllowAdd = true;
            AllowEdit = true;
            AllowDelete = true;
            AllowDeleteAllRecords = false;
            ExportFilename = null;
            AllowedExportFormats = ExportFormat.None;
        }

        public string Title { get; set; }
        public bool AllowReOrdering { get; set; }
        public bool HideSearch { get; set; }
        public bool AllowAdd { get; set; }
        public bool AllowEdit { get; set; }
        public bool AllowClone { get; set; }            // TODO: Implement logic.
        public bool AllowDelete { get; set; }
        public bool AllowDeleteAllRecords { get; set; }
        public bool ShowArchive { get; set; } = false;
        public ExportFormat AllowedExportFormats { get; set; }
        public bool AllowExcelImport { get; set; }
        public bool AllowItemSelection { get; set; }    // TODO: Implement logic.

        /// <summary>
        /// The name of the exported excel file, without extension.
        /// </summary>
        public string ExportFilename { get; set; }

        /// <summary>
        /// The name of the column to use for sorting. If empty, the OrderIndex column will be used, or the first one if we don't have sorting.
        /// </summary>
        public string DefaultSortColumnName { get; set; }

        public SortDirection DefaultSortDirection { get; set; }
        public bool ShowDeleteConfirmation { get; set; }
        public string DeletionConfirmationTitle { get; set; }
        public string DeletionConfirmationDescription { get; set; }
        public List<ListColumnInfo> Columns { get; set; }
        public List<ListAction> Actions { get; set; }
        public List<InlineFormAction> InlineFormActions { get; set; }
        public List<ListAction> SelfActions { get; set; }
        public Dictionary<string, List<SelectOption>> InputOptions { get; set; }

        /// <summary>
        /// Configuration for the dynamic banner displayed above the list
        /// </summary>
        public ListBannerConfig<dynamic> Banner { get; set; }

        /// <summary>
        /// Configuration for multiple banners displayed above the list
        /// </summary>
        public List<ListBannerConfig<dynamic>> Banners { get; set; }
        public class SelectOption
        {
            public string Display { get; set; }
            public object Value { get; set; }
        }
    }

    /// <summary>
    /// Configuration for the dynamic banner component
    /// </summary>
    public class ListBannerConfig<FormModel> where FormModel : class
    {
        public bool Enabled { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; } = "info"; // info, warning, success, error
        public string Icon { get; set; }
        public IconType? IconType { get; set; }
        public bool Closable { get; set; } = false;
        public DynamicFormStructure<FormModel> Structure { get; set; } // For dynamic form-like content
        public List<ListBannerAction> Actions { get; set; }
    }

    /// <summary>
    /// Action button configuration for banner
    /// </summary>
    public class ListBannerAction
    {
        public string Text { get; set; }
        public string Action { get; set; }
        public string Icon { get; set; }
        public IconType? IconType { get; set; }
        public string Type { get; set; } = "secondary"; // primary, secondary, success, info, warning, danger
    }

    /// <summary>
    /// Configuration for inline form actions that appear above the table
    /// </summary>
    public class InlineFormAction
    {
        public string Text { get; set; }
        public string ActionName { get; set; }
        public string Icon { get; set; }
        public IconType? IconType { get; set; }
        public string SegmentId { get; set; }
        
        // Form configuration
        public DynamicFormStructure<dynamic> FormStructure { get; set; }
        
        // Confirmation settings
        public bool RequireConfirmation { get; set; }
        public string ConfirmMessage { get; set; }
        public string ConfirmTitle { get; set; }
        public bool ShowFormDataInConfirm { get; set; } = false;
        
        // Behavior settings
        public bool ReloadData { get; set; } = true;
        public bool ShowSuccessMessage { get; set; } = true;
        public string SuccessMessageTitle { get; set; }
    }

    [Flags]
    public enum ExportFormat
    {
        None = 0,
        Excel = 1,
        Pdf = 2,
        Csv = 4
    }

    public class ListData<T>
    {
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<T> Items { get; set; }
        public Dictionary<string, List<DinoAdminConvertError>> ConversionErrors { get; set; }
    }

    public class ListRetrieveParams
    {
        public string Filter { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public List<AdvancedListFilter> AdvancedFilters { get; set; }
        public List<SortColumn> SortColumns { get; set; } // Updated to handle multiple columns
    }

    public class SortColumn
    {
        public string PropertyName { get; set; }
        public SortDirection Direction { get; set; }
    }

    public class AdvancedListFilter
    {
        public string PropertyName { get; set; }
        
        /// <summary>
        /// True for "match all" (AND), false for "match any" (OR). Defaults to true.
        /// </summary>
        [JsonProperty("matchAll")]
        public bool MatchAll { get; set; } = true;
        
        public List<FilterRule> Rules { get; set; } // Supports multiple rules per column
    }

    public enum FilterOperator
    {
        /// <summary>
        /// Starts with value (string only)
        /// </summary>
        [JsonProperty("startsWith")]
        StartsWith,

        /// <summary>
        /// Contains value (string only)
        /// </summary>
        [JsonProperty("contains")]
        Contains,

        /// <summary>
        /// Ends with value (string only)
        /// </summary>
        [JsonProperty("endsWith")]
        EndsWith,

        /// <summary>
        /// Exactly equals value
        /// </summary>
        [JsonProperty("equals")]
        Equals,

        /// <summary>
        /// Exactly equals value
        /// </summary>
        [JsonProperty("notContains")]
        NotContains,

        /// <summary>
        /// Not equals value
        /// </summary>
        [JsonProperty("notEquals")]
        NotEquals,

        /// <summary>
        /// Less than value (numbers/dates)
        /// </summary>
        [JsonProperty("lt")]
        Lt,

        /// <summary>
        /// Less than or equal to value (numbers/dates)
        /// </summary>
        [JsonProperty("lte")]
        Lte,

        /// <summary>
        /// Greater than value (numbers/dates)
        /// </summary>
        [JsonProperty("gt")]
        Gt,

        /// <summary>
        /// Greater than or equal to value (numbers/dates)
        /// </summary>
        [JsonProperty("gte")]
        Gte,

        /// <summary>
        /// Value is null
        /// </summary>
        [JsonProperty("null")]
        IsNull,

        /// <summary>
        /// Value is not null
        /// </summary>
        [JsonProperty("notNull")]
        IsNotNull,

        /// <summary>
        /// Value is in list
        /// </summary>
        [JsonProperty("in")]
        In,

        /// <summary>
        /// Date is exactly the specified value
        /// </summary>
        [JsonProperty("dateIs")]
        DateIs,

        /// <summary>
        /// Date is not the specified value
        /// </summary>
        [JsonProperty("dateIsNot")]
        DateIsNot,

        /// <summary>
        /// Date is before the specified value
        /// </summary>
        [JsonProperty("dateIsBefore")]
        DateIsBefore,

        /// <summary>
        /// Date is after the specified value
        /// </summary>
        [JsonProperty("dateIsAfter")]
        DateIsAfter,

        /// <summary>
        /// Value is between two values (inclusive)
        /// </summary>
        [JsonProperty("between")]
        Between,

        /// <summary>
        /// String is empty
        /// </summary>
        [JsonProperty("empty")]
        IsEmpty,

        /// <summary>
        /// String is not empty
        /// </summary>
        [JsonProperty("notEmpty")]
        IsNotEmpty
    }

    public class FilterRule
    {
        public FilterOperator Operator { get; set; }
        public string Value { get; set; }
        public string Value2 { get; set; } // For "between" operator
    }


    // public class DateFilter
    // {
    //     public DateTime? FromDate { get; set; }
    //     public DateTime? ToDate { get; set; }
    // }

    // public class NumericFilter
    // {
    //     public double? FromValue { get; set; }
    //     public double? ToValue { get; set; }
    // }

    #region List Actions

    public enum ListActionType
    {
        Custom = 0,
        List = 1,
        Edit = 2,
        OuterLink = 3
    }

    public enum ActionResponseType
    {
        Json = 1,
        File = 2,
        Redirect = 3
    }


    public class ListAction
    {
        public ListAction(ListActionType type, string text, string segmentId, bool passEntityId, string? actionName = null, 
                          string? icon = null, IconType? iconType = null, string? idPropertyName = null, string? idName = null,
                          bool redirect = true, bool requireConfirmation = false, DialogStructure? confirmationDialog = null, bool urlFormat = false,
                          bool reloadData = true, bool showSuccessMessage = false, string? successMessageTitle = null, bool isCustomRoute = false,
                          ActionResponseType responseType = ActionResponseType.Json, bool passModelToConfirmation = false, bool passItemSelection = false, string? itemSelectionPropertyName = null)
        {
            Type = type;
            Text = text;
            SegmentId = segmentId;
            PassEntityId = passEntityId;
            ActionName = actionName;
            Icon = icon;
            IconType = iconType;
            IdPropertyName = idPropertyName;
            IdName = idName;
            UrlFormat = urlFormat;
            Redirect = redirect;
            RequireConfirmation = requireConfirmation;
            PassModelToConfirmation = passModelToConfirmation;
            PassItemSelection = passItemSelection;
            ItemSelectionPropertyName = itemSelectionPropertyName;
            ConfirmationDialog = confirmationDialog;
            ReloadData = reloadData;
            ShowSuccessMessage = showSuccessMessage;
            SuccessMessageTitle = successMessageTitle;
            IsCustomRoute = isCustomRoute;
            ResponseType = responseType;
        }

        public ListActionType Type { get; set; }
        public string Text { get; set; }
        public string SegmentId { get; set; }
        public bool PassEntityId { get; set; }
        public string? ActionName { get; set; }
        public string? Icon { get; set; }
        public IconType? IconType { get; set; }
        public string? IdPropertyName { get; set; }
        public string? IdName { get; set; }
        public bool UrlFormat { get; set; }
        public bool Redirect { get; set; }
        public bool RequireConfirmation { get; set; }
        public bool PassModelToConfirmation { get; set; }
        public bool PassItemSelection { get; set; }
        public string? ItemSelectionPropertyName { get; set; }
        public DialogStructure? ConfirmationDialog { get; set; }
        public bool ReloadData { get; set; }
        public bool ShowSuccessMessage { get; set; }
        public string? SuccessMessageTitle { get; set; }
        public bool IsCustomRoute { get; set; }
        public ActionResponseType ResponseType { get; set; }
        // public PropertyCondition Condition { get; set; }
    }

    public class ActionFileResponse
    {
        public ActionFileResponse(byte[] data, string contentType, string fileName)
        {
            Data = Convert.ToBase64String(data);
            ContentType = contentType;
            FileName = fileName;
        }

        public string Data { get; private set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }

    public class ActionRedirectResponse
    {
        public ListActionType Type { get; set; }
        public string SegmentId { get; set; }
        public bool PassEntityId { get; set; }
        public string IdPropertyName { get; set; }
        public string IdName { get; set; }
        public bool IsCustomRoute { get; set; }
        public string ActionName { get; set; }
    }

    #endregion
}
