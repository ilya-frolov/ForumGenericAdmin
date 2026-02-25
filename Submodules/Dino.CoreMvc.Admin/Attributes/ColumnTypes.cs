namespace Dino.CoreMvc.Admin.Attributes
{
    public enum ReservedAdminPropertiesType
    {
        Sort = 1,
        Delete = 2,
        Archive = 3,
    }

    /// <summary>
    /// Marks a property to store the initial save/creation datetime of the entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SaveDateAttribute : Attribute
    {
        public SaveDateAttribute()
        {
        }
    }

    /// <summary>
    /// Marks a property to store the last update datetime of the entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)] 
    public class LastUpdateDateAttribute : Attribute
    {
        public LastUpdateDateAttribute()
        {
        }
    }

    /// <summary>
    /// Marks a property to store the sort/display order index of the entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SortIndexAttribute : Attribute
    {
        public SortIndexAttribute()
        {
        }
    }

    /// <summary>
    /// Marks a property to store the ID of the user who last updated the entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class UpdatedByAttribute : Attribute
    {
        public UpdatedByAttribute()
        {
        }
    }

    /// <summary>
    /// Marks a property that states if the item is archived or not.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArchiveIndicatorAttribute : Attribute
    {
        public ArchiveIndicatorAttribute()
        {
        }
    }

    /// <summary>
    /// Marks a property that states if the item is deleted or not.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DeletionIndicatorAttribute : Attribute
    {
        public DeletionIndicatorAttribute()
        {
        }
    }

    /// <summary>
    /// Marks a property that states if this is the reference column to the parent
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ParentReferenceColumnAttribute : Attribute
    {
        public ParentReferenceColumnAttribute()
        {
        }
    }
}