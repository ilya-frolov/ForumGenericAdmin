using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dino.Core.AdminBL.Helpers
{
    public static class DinoAdminReflectionHelpers
    {
        /// <summary>
        /// Gets the primary key properties for an entity type.
        /// Attempts to use EF Core model metadata if a DbContext is provided.
        /// Otherwise, falls back to attribute and naming conventions.
        /// </summary>
        public static IEnumerable<PropertyInfo> GetEntityKeyProperties(this Type entityType, DbContext dbContext = null)
        {
            var keyProperties = new List<PropertyInfo>();

            if (dbContext != null)
            {
                var modelEntityType = dbContext.Model.FindEntityType(entityType);
                if (modelEntityType != null)
                {
                    var primaryKey = modelEntityType.FindPrimaryKey();
                    if (primaryKey != null)
                    {
                        foreach (var property in primaryKey.Properties)
                        {
                            if (property.PropertyInfo != null)
                            {
                                keyProperties.Add(property.PropertyInfo);
                            }
                        }
                    }
                }
            }

            // If EF Core metadata didn't yield keys, or DbContext was not provided, try attribute
            if (!keyProperties.Any())
            {
                // Try to find properties with [Key] attribute
                foreach (var prop in entityType.GetProperties())
                {
                    var keyAttr = prop.GetCustomAttributes(true)
                        .FirstOrDefault(a => a.GetType().Name == "KeyAttribute");

                    if (keyAttr != null)
                    {
                        keyProperties.Add(prop);
                    }
                }
            }

            // If no [Key] attributes found, look for a property named <TypeName>Id
            if (!keyProperties.Any())
            {
                var typeNameIdProperty = entityType.GetProperty(entityType.Name + "Id");
                if (typeNameIdProperty != null)
                {
                    keyProperties.Add(typeNameIdProperty);
                }
            }

            // If no [Key] attributes or <TypeName>Id found, look for a property named "Id"
            if (!keyProperties.Any())
            {
                var idProperty = entityType.GetProperty("Id");
                if (idProperty != null)
                {
                    keyProperties.Add(idProperty);
                }
            }

            return keyProperties;
        }
    }
}
