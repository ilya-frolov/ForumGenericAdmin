namespace Dino.CoreMvc.Admin.Attributes
{
    public enum ColumnFilterType
    {
        Default,
        Contains,
        StartsWith,
        EndsWith,
        Range,
        Checkboxes
    }

    public enum MainFilterType
    {
        Default,
        DateRange,
        TextSearch,
        Dropdown
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ListSettingsAttribute : Attribute
    {
        public bool AllowSort { get; set; }
        public ColumnFilterType ColumnFilter { get; set; }
        public MainFilterType MainFilter { get; set; }
        public bool HideInTable { get; set; }
        public bool FixedColumn { get; set; }
        public bool InlineEdit { get; set; }
        public int? Priority { get; private set; }

        /// <summary>
        /// Defines list settings for a property.
        /// </summary>
        /// <param name="allowSort">Whether sorting is enabled for this column.</param>
        /// <param name="columnFilter">Defines the type of filtering available on this column.</param>
        /// <param name="mainFilter">Defines the main filter type for this property outside of the table.</param>
        /// <param name="hideInTable">Whether this column is hidden in the table view.</param>
        /// <param name="fixedColumn">Whether this column remains fixed while scrolling.</param>
        /// <param name="inlineEdit">Whether inline editing is enabled inside the table (by double-clicking the field, or by enabling editing on the table level).</param>
        /// <param name="priority">The priority of the property in the list table.</param>
        public ListSettingsAttribute(bool allowSort = true, ColumnFilterType columnFilter = ColumnFilterType.Default, MainFilterType mainFilter = MainFilterType.Default, bool hideInTable = false, 
            bool fixedColumn = false, bool inlineEdit = false, int priority = -999)
        {
            AllowSort = allowSort;
            ColumnFilter = columnFilter;
            MainFilter = mainFilter;
            HideInTable = hideInTable;
            FixedColumn = fixedColumn;
            InlineEdit = inlineEdit;

            if (priority != -999)
            {
                Priority = priority;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MultilingualSettingsAttribute : Attribute
    {
        public MultilingualSettingsAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class VisibilitySettingsAttribute : Attribute
    {
        public bool ShowOnCreate { get; set; }
        public bool ShowOnEdit { get; set; }
        public bool ShowOnView { get; set; }

        /// <summary>
        /// Defines visibility settings for a property.
        /// </summary>
        /// <param name="showOnCreate">Whether this field is shown during entity creation.</param>
        /// <param name="showOnEdit">Whether this field is shown during entity editing.</param>
        /// <param name="showOnView">Whether this field is shown in view/list modes.</param>
        public VisibilitySettingsAttribute(bool showOnCreate = true, bool showOnEdit = true, bool showOnView = true)
        {
            ShowOnCreate = showOnCreate;
            ShowOnEdit = showOnEdit;
            ShowOnView = showOnView;
        }

        /// <summary>
        /// Defines visibility settings for a property with the same setting for all modes.
        /// </summary>
        /// <param name="showInAllModes">Whether this field is shown in all form modes.</param>
        public VisibilitySettingsAttribute(bool showInAllModes)
        {
            ShowOnCreate = showInAllModes;
            ShowOnEdit = showInAllModes;
            ShowOnView = showInAllModes;
        }
    }

    /// <summary>
    /// An attribute that makes a property under another attribute to NOT be mapped under the models of the admin pages (for new and editing pages).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttributeListingInResponseAttribute : Attribute
    {
        public IgnoreAttributeListingInResponseAttribute()
        {
        }
    }
}
