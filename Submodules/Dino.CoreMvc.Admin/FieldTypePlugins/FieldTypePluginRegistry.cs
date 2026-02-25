using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Registry for field type plugins
    /// </summary>
    public class FieldTypePluginRegistry
    {
        private readonly Dictionary<Type, IFieldTypePlugin> _instances = new();
        
        // Singleton instance
        private static FieldTypePluginRegistry _instance;
        
        // Lock object for thread safety
        private static readonly object _lock = new object();
        
        /// <summary>
        /// Gets the singleton instance of the registry
        /// </summary>
        public static FieldTypePluginRegistry GetInstance(IServiceProvider serviceProvider)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new FieldTypePluginRegistry(serviceProvider);
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        /// Creates a new instance of the field type plugin registry
        /// </summary>
        private FieldTypePluginRegistry(IServiceProvider serviceProvider)
        {
            DiscoverPlugins(serviceProvider);
        }

        /// <summary>
        /// Discover plugins in the current assembly
        /// </summary>
        private void DiscoverPlugins(IServiceProvider serviceProvider)
        {
            // Get all types in the current assembly that are concrete, non-abstract, and inherit from BaseFieldTypePlugin<,>
            var baseType = typeof(BaseFieldTypePlugin<,>);
            var pluginTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => {
                    var current = t;
                    while (current != null && current != typeof(object))
                    {
                        if (current.IsGenericType &&
                            current.GetGenericTypeDefinition() == baseType)
                            return true;

                        current = current.BaseType;
                    }
                    return false;
                });


            foreach (var type in pluginTypes)
            {
                try
                {
                    // Create an instance of the plugin using the service provider from HttpContext
                    var plugin = Activator.CreateInstance(type, serviceProvider) as IFieldTypePlugin;
                    
                    if (plugin != null)
                    {
                        // Register by attribute type
                        _instances[plugin.AttributeType] = plugin;
                    }
                }
                catch
                {
                    // Skip if instantiation fails
                }
            }
        }

        /// <summary>
        /// Manually register a plugin with the registry
        /// </summary>
        public void Register<TAttribute, TValue>(BaseFieldTypePlugin<TAttribute, TValue> instance) 
            where TAttribute : AdminFieldBaseAttribute
        {
            _instances[typeof(TAttribute)] = instance;
        }

        /// <summary>
        /// Gets a plugin for a specific attribute type with strong typing (when you know the types)
        /// </summary>
        public BaseFieldTypePlugin<TAttribute, TValue> GetPlugin<TAttribute, TValue>() 
            where TAttribute : AdminFieldBaseAttribute
        {
            if (_instances.TryGetValue(typeof(TAttribute), out var instance))
            {
                if (instance is BaseFieldTypePlugin<TAttribute, TValue> typedPlugin)
                {
                    return typedPlugin;
                }
            }
            return null;
        }

        /// <summary>
        /// Get a plugin by attribute type (when you don't know the exact types)
        /// </summary>
        public IFieldTypePlugin GetPlugin(Type attributeType)
        {
            if (_instances.TryGetValue(attributeType, out var instance))
            {
                return instance;
            }
            return null;
        }

        /// <summary>
        /// Get a plugin by field attribute (when you don't know the exact types)
        /// </summary>
        public IFieldTypePlugin GetPlugin(AdminFieldBaseAttribute fieldAttribute)
        {
            if (fieldAttribute == null)
                return null;
                
            return GetPlugin(fieldAttribute.GetType());
        }

        /// <summary>
        /// Get a plugin by field type name (when you don't know the exact types)
        /// </summary>
        public IFieldTypePlugin GetPluginByName(string fieldTypeName)
        {
            if (string.IsNullOrEmpty(fieldTypeName))
                return null;
                
            // Search all plugins for one with a matching field type
            foreach (var plugin in _instances.Values)
            {
                if (string.Equals(plugin.FieldType, fieldTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    return plugin;
                }
            }
            
            return null;
        }
    }
} 