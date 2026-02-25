using System;
using System.Reflection;

namespace Dino.CoreMvc.Admin.Logic
{
    /// <summary>
    /// Extension methods for PropertyInfo to work with attributes more easily.
    /// </summary>
    public static class PropertyInfoExtensions
    {
        /// <summary>
        /// Gets an attribute of the specified type from a property, or null if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The attribute type to retrieve</typeparam>
        /// <param name="property">The property to get the attribute from</param>
        /// <param name="inherit">Whether to search the inheritance chain for attributes</param>
        /// <returns>The attribute of the specified type, or null if not found</returns>
        public static T GetAttribute<T>(this PropertyInfo property, bool inherit = false) where T : Attribute
        {
            return property.GetCustomAttribute<T>(inherit);
        }

        /// <summary>
        /// Checks if a property has an attribute of the specified type.
        /// </summary>
        /// <typeparam name="T">The attribute type to check for</typeparam>
        /// <param name="property">The property to check</param>
        /// <param name="inherit">Whether to search the inheritance chain for attributes</param>
        /// <returns>True if the property has the attribute, false otherwise</returns>
        public static bool HasAttribute<T>(this PropertyInfo property, bool inherit = false) where T : Attribute
        {
            return property.GetAttribute<T>(inherit) != null;
        }

        /// <summary>
        /// Gets a property with the specified attribute from a type, or null if not found.
        /// </summary>
        /// <typeparam name="T">The attribute type to search for</typeparam>
        /// <param name="type">The type to search in</param>
        /// <param name="inherit">Whether to search the inheritance chain for attributes</param>
        /// <returns>The first property with the specified attribute, or null if not found</returns>
        public static PropertyInfo GetPropertyWithAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {
            foreach (var property in type.GetProperties())
            {
                if (property.HasAttribute<T>(inherit))
                {
                    return property;
                }
            }
            return null;
        }
    }
} 