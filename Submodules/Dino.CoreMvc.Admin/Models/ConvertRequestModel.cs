using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Dino.CoreMvc.Admin.Models
{
    public class DinoAdminConvertError
    {
        public string PropertyPath { get; set; }
        public string PropertyName { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
    }

    /// <summary>
    /// Model for save requests
    /// </summary>
    public class DinoAdminConvertRequestModel
    {
        /// <summary>
        /// Gets or sets the entity ID for edit mode (null for create)
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets whether this is a new entity
        /// </summary>
        public bool IsNew { get; set; }
        
        /// <summary>
        /// Gets or sets the model data as a JSON object
        /// </summary>
        public JObject Model { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata for the save operation.
        /// This can include contextual information that isn't part of the entity 
        /// but is needed to properly process the save request.
        /// 
        /// Examples include:
        /// - UI state (collapsed/expanded sections, active tabs)
        /// - User preferences (sort order, filter settings)
        /// - Operation context (save and continue vs. save and close)
        /// - Related entity information (parent context)
        /// - Temporary file uploads pending association
        /// - Display language for validation errors
        /// 
        /// This metadata is not stored directly in the database as part of the entity,
        /// but can influence how the entity is saved or processed.
        /// </summary>
        public JObject Metadata { get; set; }
    }

    /// <summary>
    /// Result of a model convert operation
    /// </summary>
    public class ModelConvertResult
    {
        /// <summary>
        /// Gets or sets whether the save was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Gets or sets the error message if save failed
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the entity ID after save
        /// </summary>
        public string EntityId { get; set; }
        
        /// <summary>
        /// Gets or sets the validation errors by property name
        /// </summary>
        public Dictionary<string, List<DinoAdminConvertError>> ValidationErrors { get; set; }
    }
} 