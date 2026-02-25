using System;

namespace Dino.CoreMvc.Admin.Attributes
{
    /// <summary>
    /// Marks a property to be skipped during model to database entity mapping and vice versa.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SkipMappingAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to skip mapping from database to model.
        /// </summary>
        public bool SkipFromDb { get; set; }

        /// <summary>
        /// Gets or sets whether to skip mapping from model to database.
        /// </summary>
        public bool SkipToDb { get; set; }

        /// <summary>
        /// Gets or sets whether to skip mapping null values to existing properties in the database entity.
        /// </summary>
        public bool SkipNullValues { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the property during import operations.
        /// </summary>
        public bool SkipImport { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the property during export operations.
        /// </summary>
        public bool SkipExport { get; set; }

        /// <summary>
        /// Marks a property to be skipped during model mapping operations.
        /// </summary>
        /// <param name="skipFromDb">Whether to skip mapping from database to model.</param>
        /// <param name="skipToDb">Whether to skip mapping from model to database.</param>
        /// <param name="skipNullValues">Whether to skip mapping null values to existing properties in the database entity.</param>
        /// <param name="skipImport">Whether to skip the property during import operations.</param>
        /// <param name="skipExport">Whether to skip the property during export operations.</param>
        public SkipMappingAttribute(bool skipFromDb = false, bool skipToDb = false, bool skipNullValues = false, bool skipImport = false, bool skipExport = false)
        {
            SkipFromDb = skipFromDb;
            SkipToDb = skipToDb;
            SkipNullValues = skipNullValues;
            SkipImport = skipImport;
            SkipExport = skipExport;
        }
    }
} 