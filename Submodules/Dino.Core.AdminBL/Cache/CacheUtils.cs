using System;
using System.Collections.Generic;
using System.Linq;
using Dino.CoreMvc.Admin.Models.Admin;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;

namespace Dino.Core.AdminBL.Cache
{
    public static class CacheUtils
    {
        // Stores CacheModelAttribute keyed by the Cache Model Type
        private static readonly Dictionary<Type, CacheModelAttribute> _cacheAttributes = new Dictionary<Type, CacheModelAttribute>();
        // Maps Cache Model Type to its corresponding DB Model Type
        private static readonly Dictionary<Type, Type> _cacheToDbType = new Dictionary<Type, Type>();
        // Stores all known Cache Model Types for quick checking
        private static readonly HashSet<Type> _cacheModelTypes = new HashSet<Type>();

        public static void InitializeCacheTypes()
        {
            _cacheAttributes.Clear();
            _cacheToDbType.Clear();
            _cacheModelTypes.Clear();

            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var allTypes = new List<Type>();

            // Iterate through assemblies and safely get types
            foreach (var assembly in allAssemblies)
            {
                try
                {
                    allTypes.AddRange(assembly.GetTypes());
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Log detailed information about the loading errors
                    // Consider using a proper logging framework here instead of Console.WriteLine
                    // Console.WriteLine($"[CacheUtils] Warning: Could not load all types from assembly: {assembly.FullName}. Error: {ex.Message}");
                    foreach (var loaderException in ex.LoaderExceptions ?? Enumerable.Empty<Exception>())
                    {
                        // Console.WriteLine($"[CacheUtils]   LoaderException: {loaderException?.Message}");
                    }
                    // Add the types that were loaded successfully
                    var loadedTypes = ex.Types.Where(t => t != null);
                    allTypes.AddRange(loadedTypes);
                }
                catch(Exception ex)
                {
                    // Catch other potential exceptions during type loading
                }
            }

            // Find types decorated with CacheModelAttribute from the successfully loaded types
            var cacheModelTypes = allTypes // Use the combined list
                .Where(t => t != null && t.IsClass && !t.IsAbstract && t.GetCustomAttribute<CacheModelAttribute>() != null);

            foreach (var cacheType in cacheModelTypes)
            {
                var attribute = cacheType.GetCustomAttribute<CacheModelAttribute>();
                _cacheAttributes[cacheType] = attribute;
                _cacheModelTypes.Add(cacheType);

                if (attribute.DbModelType != null)
                {
                    _cacheToDbType[cacheType] = attribute.DbModelType;
                }
            }
        }

        public static Type GetDbType(Type cacheType)
        {
            return _cacheToDbType.TryGetValue(cacheType, out var dbType) ? dbType : null;
        }

        public static CacheModelAttribute GetCacheAttribute(Type cacheType)
        {
            return _cacheAttributes.TryGetValue(cacheType, out var attribute) ? attribute : null;
        }

        public static string GetCacheKey(Type cacheType, object id)
        {
            return $"{cacheType.Name}_{id}";
        }

        public static MemoryCacheEntryOptions CreateCacheOptions(CacheModelAttribute attribute, TimeSpan? specificExpiration = null)
        {
            var options = new MemoryCacheEntryOptions();
            
            if (specificExpiration.HasValue)
            {
                if (attribute.UseSlidingExpiration)
                    options.SetSlidingExpiration(specificExpiration.Value);
                else
                    options.SetAbsoluteExpiration(specificExpiration.Value);
            }

            return options;
        }

        public static bool IsCacheModel(Type type)
        {
            return _cacheModelTypes.Contains(type);
        }

        public static IEnumerable<Type> GetAllCacheModelTypes()
        {
            return _cacheModelTypes.ToList();
        }

        public static IEnumerable<Type> GetCacheModelsToLoadOnStartup()
        {
            return _cacheAttributes
                .Where(kvp => kvp.Value.CacheTiming == CacheTiming.OnApplicationStart && kvp.Value.DbModelType != null)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Configures AutoMapper mappings for registered cache models where ManualMapping is false.
        /// </summary>
        /// <param name="cfg">The AutoMapper configuration expression.</param>
        public static void ConfigureAutoMapperCacheMappings(IMapperConfigurationExpression cfg)
        {
            var allCacheTypes = GetAllCacheModelTypes();

            foreach (var cacheType in allCacheTypes)
            {
                var attribute = GetCacheAttribute(cacheType);
                if (attribute?.DbModelType == null)
                {
                    // Console.WriteLine($"[CacheUtils] Warning: Skipping AutoMapper config for {cacheType.Name} - DbModelType not specified.");
                    continue;
                }

                // Skip if ManualMapping is explicitly set to true
                if (attribute.ManualMapping)
                {
                    // Console.WriteLine($"[CacheUtils] Skipping AutoMapper config for {cacheType.Name} - ManualMapping set to true.");
                    continue;
                }

                var dbType = attribute.DbModelType;

                // Register standard mapping
                try
                {
                    cfg.CreateMap(dbType, cacheType);
                    // Console.WriteLine($"[CacheUtils] Configured STANDARD AutoMapper: {dbType.Name} -> {cacheType.Name}");
                }
                catch (Exception ex)
                {
                    // Console.WriteLine($"[CacheUtils] Error configuring STANDARD AutoMapper for {dbType.Name} -> {cacheType.Name}: {ex.Message}");
                    // Decide if we should throw or just log
                }
            }
        }
    }
} 