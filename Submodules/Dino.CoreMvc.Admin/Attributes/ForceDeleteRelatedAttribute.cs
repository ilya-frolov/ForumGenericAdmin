using System;

namespace Dino.CoreMvc.Admin.Attributes
{
    /// <summary>
    /// When applied to a repeater property, this attribute indicates that related entities
    /// should be forcefully deleted when removed from the repeater, even if they have relations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ForceDeleteRelatedAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to cascade delete related entities
        /// </summary>
        public bool CascadeDelete { get; set; }
        
        /// <summary>
        /// Creates a new instance of ForceDeleteRelatedAttribute
        /// </summary>
        /// <param name="cascadeDelete">Whether to cascade delete related entities</param>
        public ForceDeleteRelatedAttribute(bool cascadeDelete = false)
        {
            CascadeDelete = cascadeDelete;
        }
    }
} 