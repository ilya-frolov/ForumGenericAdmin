using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Dino.Core.AdminBL.Contracts;
using System.Threading.Tasks;
using Dino.Core.AdminBL;
using Dino.Core.AdminBL.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dino.Infra.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Dino.CoreMvc.Admin.Models.Admin;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using Dino.Core.AdminBL.Helpers;

namespace Dino.Core.AdminBL.Cache
{
    public abstract class BaseDinoCacheManager<TDbContext, TBlConfig, TCacheManager> : IDinoCacheManager
        where TDbContext : BaseDbContext<TDbContext>
        where TBlConfig : BaseBlConfig
        where TCacheManager : BaseDinoCacheManager<TDbContext, TBlConfig, TCacheManager>
    {
        // Dependencies
        protected readonly IConfiguration _config;
        protected readonly IMapper _mapper;
        protected readonly IOptions<TBlConfig> _blConfig;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ThreadSafeFlexiCacheManager _cacheManager;
        protected BLFactory<TDbContext, TBlConfig, TCacheManager> _blFactory;

        // Store default expiration for easier access
        private readonly TimeSpan _defaultExpiration;

        protected BaseDinoCacheManager(IConfiguration config, IMapper mapper, IOptions<TBlConfig> blConfig, IServiceProvider serviceProvider)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _blConfig = blConfig ?? throw new ArgumentNullException(nameof(blConfig));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Validate CacheConfig exists
            var cacheConfig = _blConfig.Value.CacheConfig ?? throw new InvalidOperationException("CacheConfig is missing in application settings.");

            // Use CacheConfig from BlConfig
            _defaultExpiration = cacheConfig.DefaultExpiration;

            var cacheOptions = new DynamicCacheOptions
            {
                UseRedis = cacheConfig.UseRedis,
                RedisHost = cacheConfig.RedisHost,
                RedisPort = cacheConfig.RedisPort,
                RedisPassword = cacheConfig.RedisPassword,
                RedisDatabase = cacheConfig.RedisDatabase,
                DefaultExpiration = cacheConfig.DefaultExpiration
            };

            _cacheManager = new ThreadSafeFlexiCacheManager(cacheOptions);

            // Initialize cache types.
            CacheUtils.InitializeCacheTypes();
        }

        // Helper to determine the correct expiration timespan
        private TimeSpan GetEffectiveExpiration(CacheModelAttribute attribute)
        {
            // If attribute expiration is Zero (our sentinel), use the manager's default, otherwise use the attribute's value.
            return attribute.Expiration == 0 ? _defaultExpiration : new TimeSpan(0, 0, 0, attribute.Expiration);
        }

        private PropertyInfo GetPrimaryKeyPropertyInfo(Type entityType)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));

            var keyProperties = entityType.GetEntityKeyProperties().ToList();
            if (!keyProperties.Any())
                throw new InvalidOperationException($"Could not find any key properties on type {entityType.Name} using ModelMappingExtensions.GetEntityKeyProperties.");
            if (keyProperties.Count > 1)
                // This assumption is consistent with current usage. 
                // If composite keys need to be handled differently by these methods, this logic would need to be expanded.
                System.Diagnostics.Debug.WriteLine($"Warning: Found multiple key properties on type {entityType.Name}. Using the first one found for cache operations.");
            
            return keyProperties.First();
        }

        private object GetEntityIdValue(object entity)
        {
            if (entity == null) return null;
            var idProperty = GetPrimaryKeyPropertyInfo(entity.GetType());
            return idProperty?.GetValue(entity);
        }

        protected BLFactory<TDbContext, TBlConfig, TCacheManager> CreateBLFactory()
        {
            return new BLFactory<TDbContext, TBlConfig, TCacheManager>(_config, _blConfig, _serviceProvider, _mapper);
        }

        protected BLFactory<TDbContext, TBlConfig, TCacheManager> GetBLFactory()
        {
            if (_blFactory == null)
            {
                _blFactory = CreateBLFactory();
            }

            return _blFactory;
        }

        protected TDbContext GetNewDbContext()
        {
            var blFactory = GetBLFactory();
            return blFactory.GetNewContext();
        }

        public virtual async Task LoadAll()
        {
            var tasks = CacheUtils.GetCacheModelsToLoadOnStartup()
                .Select(cacheType => LoadAllForType(cacheType));
            await Task.WhenAll(tasks);
        }

        protected async Task LoadAllForType(Type cacheType)
        {
            var dbType = CacheUtils.GetDbType(cacheType);
            if (dbType == null)
                return;

            var attribute = CacheUtils.GetCacheAttribute(cacheType);
            if (attribute == null)
                return;

            // Create a scope to resolve the DbContext
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

            // Use the scoped dbContext instance for reflection target
            var dbSetMethodInfo = typeof(TDbContext)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == "Set" &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 0 &&
                    m.GetGenericArguments().Length == 1 // Ensures it's Set<TEntity>
                );

            if (dbSetMethodInfo == null)
            {
                 throw new InvalidOperationException($"Could not find generic Set<TEntity>() method on {typeof(TDbContext).Name}");
            }

            var genericSetMethod = dbSetMethodInfo.MakeGenericMethod(dbType);
            // Use the scoped dbContext here
            var dbSet = genericSetMethod.Invoke(dbContext, null);

            // Get AsNoTracking extension method
            var asNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethod("AsNoTracking", BindingFlags.Public | BindingFlags.Static)
                .MakeGenericMethod(dbType);

            var queryableDbSet = (IQueryable)dbSet;
            var noTrackingDbSet = (IQueryable)asNoTrackingMethod.Invoke(null, new object[] { queryableDbSet });

            // Efficiently load all entities - Use the scoped dbContext via the queryable
            var toListAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethod("ToListAsync", BindingFlags.Public | BindingFlags.Static)
                .MakeGenericMethod(dbType);

            // Invoke ToListAsync on the queryable derived from the scoped context
            var entitiesTask = (Task)toListAsyncMethod.Invoke(null, new object[] { noTrackingDbSet, default(System.Threading.CancellationToken) });
            await entitiesTask;

            var entities = (System.Collections.IEnumerable)entitiesTask.GetType().GetProperty("Result").GetValue(entitiesTask);

            // Map to cache models and cache them
            foreach (var entity in entities)
            {
                var id = GetEntityIdValue(entity);
                if (id == null)
                    continue;

                // UpdateFromDbModel will create its own scope if needed for FindAsync
                await Update(cacheType, id, entity);
            }
            // Scope (and dbContext) will be disposed automatically here
        }

        /// <summary>
        /// Public method to trigger a reload of all items for a specific cache type.
        /// Useful for scenarios like sort order changes.
        /// Clears the type's region before reloading.
        /// </summary>
        public async Task ReloadCacheForType(Type cacheType)
        {
            if (!CacheUtils.IsCacheModel(cacheType)) return;

            // Clear the region first using reflection
            try
            {
                var clearRegionMethod = typeof(ThreadSafeFlexiCacheManager)
                    .GetMethod("ClearRegion", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (clearRegionMethod != null)
                {
                    var genericClearRegion = clearRegionMethod.MakeGenericMethod(cacheType);
                    genericClearRegion.Invoke(_cacheManager, null);
                }
            }
            catch (Exception ex)
            {
                // Decide if we should proceed with LoadAllForType even if clearing failed.
                // For now, let's proceed.
            }

            // Now reload the data
            await LoadAllForType(cacheType);
        }

        /// <summary>
        /// Reloads all items for a specific cache type (identified by TCache), typically clearing the region first.
        /// </summary>
        public Task ReloadCacheForType<TCache>() where TCache : class
        {
            return ReloadCacheForType(typeof(TCache));
        }

        #region Reflection Helpers for Cache Manager

        private void InvokeCacheSet(Type cacheType, object id, object cacheModel, MemoryCacheEntryOptions options)
        {
            var method = typeof(ThreadSafeFlexiCacheManager).GetMethod("Set")
                         .MakeGenericMethod(cacheType, id.GetType());
            method.Invoke(_cacheManager, new[] { id, cacheModel, options, false /* ignoreLock */ });
        }

        private void InvokeCacheRemove<TId>(Type cacheType, TId id)
        {
            if (EqualityComparer<TId>.Default.Equals(id, default)) return;

            var method = typeof(ThreadSafeFlexiCacheManager).GetMethod("Remove")
                         .MakeGenericMethod(cacheType, typeof(TId));
            method.Invoke(_cacheManager, new object[] { id });
        }

        #endregion

        #region DB Model Operations (Renamed)

        /// <summary>
        /// Retrieves an item by its identifier, creating it from the DB model if necessary.
        /// </summary>
        public async Task<TCache> GetOrCreate<TCache, TId>(TId id, object dbModel = null) where TCache : class
        {
            var cacheType = typeof(TCache);
            if (!CacheUtils.IsCacheModel(cacheType)) return default;
            if (EqualityComparer<TId>.Default.Equals(id, default)) return default;

            var attribute = CacheUtils.GetCacheAttribute(cacheType);
            if (attribute == null) return default;

            var effectiveExpiration = GetEffectiveExpiration(attribute);
            var options = CacheUtils.CreateCacheOptions(attribute, effectiveExpiration);

            // Use GetOrCreateAsync with TCache and TId
            return await _cacheManager.GetOrCreateAsync<TCache, TId>(id, async (entry) =>
            {
                // Create a scope *inside the factory* to resolve DbContext
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

                // Use provided dbModel or load from DB
                object entity = dbModel;
                if (entity == null)
                {
                    var dbType = CacheUtils.GetDbType(cacheType);
                    if (dbType == null)
                    {
                        return default;
                    }

                    // Use the locally scoped dbContext for reflection target
                    var dbSetMethodInfo = typeof(TDbContext)
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(m =>
                            m.Name == "Set" &&
                            m.IsGenericMethodDefinition &&
                            m.GetParameters().Length == 0 &&
                            m.GetGenericArguments().Length == 1 // Ensures it's Set<TEntity>
                        );

                    if (dbSetMethodInfo == null)
                    {
                         throw new InvalidOperationException($"Could not find generic Set<TEntity>() method on {typeof(TDbContext).Name}");
                    }

                    var genericSetMethod = dbSetMethodInfo.MakeGenericMethod(dbType);
                    // Use the scoped dbContext here
                    var dbSet = genericSetMethod.Invoke(dbContext, null);

                    var findMethod = typeof(DbSet<>).MakeGenericType(dbType).GetMethod("FindAsync", new[] { typeof(object[]) });

                    // Await the result directly; await handles the ValueTask<TEntity>
                    // The result of await will be the entity object or null.
                    entity = await (dynamic)findMethod.Invoke(dbSet, new object[] { new object[] { id } });
                }

                if (entity == null)
                {
                    return default;
                }

                // Map DB entity to Cache model using AutoMapper ONLY
                return _mapper.Map<TCache>(entity);
            }, options);
        }

        /// <summary>
        /// Updates a specific Cache Model instance (identified by TCache) from a DB model.
        /// </summary>
        public Task Update<TCache>(object id, object dbModel) where TCache : class
        {
            return Update(typeof(TCache), id, dbModel);
        }

        /// <summary>
        /// Updates a specific Cache Model instance from a DB model.
        /// </summary>
        public async Task Update(Type cacheType, object id, object dbModel)
        {
            if (!CacheUtils.IsCacheModel(cacheType)) return;
            if (id == null || dbModel == null) return;

            var attribute = CacheUtils.GetCacheAttribute(cacheType);
            if (attribute == null) return;

            var effectiveExpiration = GetEffectiveExpiration(attribute);
            var options = CacheUtils.CreateCacheOptions(attribute, effectiveExpiration);
            object cacheModelResult = null;

            // Always use AutoMapper now
            cacheModelResult = _mapper.Map(dbModel, dbModel.GetType(), cacheType);

            if (cacheModelResult != null)
            {
                InvokeCacheSet(cacheType, id, cacheModelResult, options);
            }
        }

        /// <summary>
        /// Removes a specific Cache Model instance (identified by type) from cache, using its ID.
        /// </summary>
        public void Remove<TId>(Type cacheType, TId id)
        {
            if (!CacheUtils.IsCacheModel(cacheType)) return;
            if (EqualityComparer<TId>.Default.Equals(id, default)) return;

            var attribute = CacheUtils.GetCacheAttribute(cacheType);
            if (attribute == null) return; // Or proceed anyway?

            // Use reflection helper, passing the strongly typed TId
            InvokeCacheRemove<TId>(cacheType, id);
        }

        /// <summary>
        /// Removes a specific Cache Model instance (identified by TCache) from cache, using its ID.
        /// </summary>
        public void Remove<TCache, TId>(TId id) where TCache : class
        {
            Remove<TId>(typeof(TCache), id);
        }

        /// <summary>
        /// Reloads cache entries for a given Cache Type from a list of DB models.
        /// </summary>
        public async Task ReloadItems(Type cacheType, IEnumerable<object> dbModels)
        {
            if (!CacheUtils.IsCacheModel(cacheType)) return;
            if (dbModels == null) return;

            var attribute = CacheUtils.GetCacheAttribute(cacheType);
            if (attribute == null) return;

            foreach (var dbModel in dbModels.Where(m => m != null))
            {
                var id = dbModel.GetType().GetProperty("Id")?.GetValue(dbModel);
                if (id == null) continue;

                // Calls the renamed Update method
                await Update(cacheType, id, dbModel);
            }
        }

        #endregion

        /// <summary>
        /// Updates cache entries for a specific list of IDs.
        /// Fetches the latest DB state for each ID and updates the corresponding cache entry.
        /// Optimized to fetch entities in a single DB query.
        /// </summary>
        public async Task UpdateCacheForItems<TId>(Type cacheType, IEnumerable<TId> ids)
        {
            if (!CacheUtils.IsCacheModel(cacheType) || ids == null) return;
            var idList = ids.Distinct().ToList(); // Ensure unique IDs and have a list for Contains
            if (!idList.Any()) return;

            var dbType = CacheUtils.GetDbType(cacheType);
            if (dbType == null) return;

            Dictionary<TId, object> dbModelsDict = new Dictionary<TId, object>();

            // Create ONE scope for the database operation
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

                // 1. Get the DbSet<TEntity>
                var dbSetMethodInfo = typeof(TDbContext).GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "Set" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0 && m.GetGenericArguments().Length == 1);
                if (dbSetMethodInfo == null) throw new InvalidOperationException($"Could not find generic Set<TEntity>() method on {typeof(TDbContext).Name}");
                var genericSetMethod = dbSetMethodInfo.MakeGenericMethod(dbType);
                var dbSetQueryable = (IQueryable)genericSetMethod.Invoke(dbContext, null);

                // 2. Build the dynamic Where clause: x => idList.Contains(x.Id)
                var parameter = Expression.Parameter(dbType, "x");
                
                var idPropertyInfo = GetPrimaryKeyPropertyInfo(dbType);
                if (idPropertyInfo.PropertyType != typeof(TId))
                    throw new InvalidOperationException($"The determined key property '{idPropertyInfo.Name}' type '{idPropertyInfo.PropertyType.Name}' does not match the generic ID type '{typeof(TId).Name}'.");

                var propertyAccess = Expression.Property(parameter, idPropertyInfo);
                var idsConstant = Expression.Constant(idList);
                // Ensure Contains method is retrieved for the correct List<TId> type.
                var containsMethod = typeof(List<>).MakeGenericType(typeof(TId)).GetMethod("Contains", new[] { typeof(TId) });
                if (containsMethod == null) throw new InvalidOperationException($"Could not find Contains method on List<{typeof(TId).Name}>.");

                var containsCall = Expression.Call(idsConstant, containsMethod, propertyAccess);
                var lambda = Expression.Lambda(containsCall, parameter);

                // 3. Apply the Where clause
                var whereMethod = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                                                .MakeGenericMethod(dbType);
                var filteredQuery = (IQueryable)whereMethod.Invoke(null, new object[] { dbSetQueryable, lambda });

                // 4. Execute the query to get all matching entities
                var toListMethod = typeof(EntityFrameworkQueryableExtensions).GetMethod("ToListAsync", BindingFlags.Static | BindingFlags.Public)
                                                                    .MakeGenericMethod(dbType);
                var resultsTask = (Task)toListMethod.Invoke(null, new object[] { filteredQuery, default(System.Threading.CancellationToken) });
                await resultsTask;

                var dbEntities = (IEnumerable)resultsTask.GetType().GetProperty("Result").GetValue(resultsTask);

                // 5. Populate the dictionary for quick lookup
                foreach (var entity in dbEntities)
                {
                    // Use the same idPropertyInfo obtained earlier
                    var entityId = (TId)idPropertyInfo.GetValue(entity);
                    dbModelsDict[entityId] = entity;
                }
                // Scope (and dbContext) disposed here

                // 6. Iterate through the *original* list of IDs to update/remove cache entries
                foreach (var id in idList)
                {
                    if (dbModelsDict.TryGetValue(id, out var dbModel))
                    {
                        // Found in DB, update cache
                        await Update(cacheType, id, dbModel); // Pass original TId
                    }
                    else
                    {   // Not found in DB (was in input list but not in results), remove from cache
                        Remove<TId>(cacheType, id); // Pass original TId
                    }
                }
            }


        }

        /// <summary>
        /// Updates cache entries for a specific list of IDs (identified by TCache).
        /// Fetches the latest DB state for each ID and updates the corresponding cache entry.
        /// </summary>
        public Task UpdateCacheForItems<TCache, TId>(IEnumerable<TId> ids) where TCache : class
        {
            return UpdateCacheForItems<TId>(typeof(TCache), ids);
        }

        /// <summary>
        /// Updates cache entries for a specific list of IDs (objects).
        /// Determines the ID type from the cacheType and calls the generic version.
        /// </summary>
        public async Task UpdateCacheForItems(Type cacheType, IEnumerable<object> ids)
        {
            if (!CacheUtils.IsCacheModel(cacheType) || ids == null) return;
            
            var objectIdList = ids.Where(id => id != null).Distinct().ToList();
            if (!objectIdList.Any()) return;

            var dbType = CacheUtils.GetDbType(cacheType);
            if (dbType == null) return;

            var idPropertyInfo = GetPrimaryKeyPropertyInfo(dbType);
            var idType = idPropertyInfo.PropertyType;

            // Convert IEnumerable<object> to IEnumerable<idType>
            var typedIdList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(idType));
            foreach (var objId in objectIdList)
            {
                try
                {
                    var convertedId = Convert.ChangeType(objId, idType);
                    typedIdList.Add(convertedId);
                }
                catch (Exception ex)
                {
                    // Log or handle conversion errors. For now, skipping unconvertible IDs.
                    System.Diagnostics.Debug.WriteLine($"Error converting ID '{objId}' to type '{idType.Name}': {ex.Message}");
                    continue; 
                }
            }

            if (!typedIdList.Cast<object>().Any()) return; // No convertible IDs found

            // Get the generic method definition UpdateCacheForItems<TId>(Type, IEnumerable<TId>)
            var genericMethodDefinition = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(UpdateCacheForItems) && 
                                     m.IsGenericMethodDefinition &&
                                     m.GetGenericArguments().Length == 1 && // Ensures it's the <TId> version
                                     m.GetParameters().Length == 2 &&
                                     m.GetParameters()[0].ParameterType == typeof(Type) &&
                                     m.GetParameters()[1].ParameterType.IsGenericType &&
                                     m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                                     m.GetParameters()[1].ParameterType.GetGenericArguments()[0].IsGenericParameter); // Ensures the IEnumerable<T> is for the generic TId

            if (genericMethodDefinition == null)
            {
                throw new InvalidOperationException($"Could not find the generic method definition for {nameof(UpdateCacheForItems)}<TId>(Type, IEnumerable<TId>).");
            }
            
            // Make the generic method with the actual idType
            var concreteMethod = genericMethodDefinition.MakeGenericMethod(idType);
            
            var task = (Task)concreteMethod.Invoke(this, new object[] { cacheType, typedIdList });
            await task;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cacheManager?.Dispose();
            }
        }

         ~BaseDinoCacheManager()
        {
            Dispose(false);
        }
    }
} 