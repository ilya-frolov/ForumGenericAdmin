namespace Dino.CoreMvc.Admin.Attributes
{
    public enum ContainerBehavior
    {
        None = 1,
        Collapsible = 2,
        Tab = 3
    }

    [AttributeUsage(AttributeTargets.Property)]
    public abstract class SectionAttribute : Attribute
    {
        [IgnoreAttributeListingInResponse]
        public string Title { get; set; }

        /// <summary>
        /// Defines a section grouping multiple properties visually.
        /// </summary>
        /// <param name="title">Title of the section.</param>
        public SectionAttribute(string title)
        {
            Title = title;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public abstract class EndSectionAttribute : Attribute
    {
        [IgnoreAttributeListingInResponse]
        public string Title { get; set; }

        /// <summary>
        /// Defines an ending of a section.
        /// </summary>
        /// <param name="title">Optional. Just for clean-code purposes, and the ability to close 2 containers on the same property.</param>
        public EndSectionAttribute(string title = null)
        {
            Title = title;
        }
    }

    public enum ContainerDisplayMode
    {
        Standard,
        Collapsible
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ContainerAttribute : SectionAttribute
    {
        public string SubTitle { get; set; }
        public FieldWidth Width { get; set; }
        public ContainerDisplayMode DisplayMode { get; set; }
        public bool DefaultCollapsed { get; set; }
        public string ContainerId { get; set; }

        /// <summary>
        /// Defines a container that groups properties inside a section.
        /// </summary>
        /// <param name="title">Title of the container.</param>
        /// <param name="subTitle">Subtitle of the container</param>
        /// <param name="width">Width of the container (e.g., 50%, 100%).</param>
        /// <param name="displayMode">Defines the container style.</param>
        /// <param name="defaultCollapsed">If displayMode is Collapsible, determines default state.</param>
        /// <param name="containerId">Unique identifier for referencing visibility conditions.</param>
        public ContainerAttribute(string title, string subTitle = null, FieldWidth width = FieldWidth.Full, ContainerDisplayMode displayMode = ContainerDisplayMode.Standard, bool defaultCollapsed = false, string containerId = null) : base(title)
        {
            SubTitle = subTitle;
            Width = width;
            DisplayMode = displayMode;
            DefaultCollapsed = defaultCollapsed;
            ContainerId = containerId;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class TabAttribute : SectionAttribute
    {
        /// <summary>
        /// Defines a tab for visually grouping properties inside a UI.
        /// </summary>
        /// <param name="title">Title of the tab.</param>
        public TabAttribute(string title) : base(title) { }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class EndContainerAttribute : EndSectionAttribute
    {
        /// <summary>
        /// Marks the end of a container.
        /// </summary>
        public EndContainerAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class EndTabAttribute : EndSectionAttribute
    {
        /// <summary>
        /// Marks the end of a tab.
        /// </summary>
        public EndTabAttribute() { }
    }
}
