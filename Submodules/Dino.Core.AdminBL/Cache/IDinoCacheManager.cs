using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dino.Core.AdminBL.Cache
{
    /// <summary>
    /// Defines the contract for a cache manager responsible for handling cached application data.
    /// </summary>
    public interface IDinoCacheManager : IDisposable
    {
        /// <summary>
        /// Loads cache models marked for startup loading.
        /// </summary>
        Task LoadAll();

        /// <summary>
        /// Retrieves an item by its identifier, creating it from the DB model if necessary.
        /// </summary>
        Task<TCache> GetOrCreate<TCache, TId>(TId id, object dbModel = null) where TCache : class;

        /// <summary>
        /// Updates a specific Cache Model instance (identified by type) from a DB model.
        /// </summary>
        Task Update(Type cacheType, object id, object dbModel);

        /// <summary>
        /// Updates a specific Cache Model instance (identified by TCache) from a DB model.
        /// </summary>
        Task Update<TCache>(object id, object dbModel) where TCache : class;

        /// <summary>
        /// Removes a specific Cache Model instance (identified by type) from cache, using its ID.
        /// </summary>
        void Remove<TId>(Type cacheType, TId id);

        /// <summary>
        /// Removes a specific Cache Model instance (identified by TCache) from cache, using its ID.
        /// </summary>
        void Remove<TCache, TId>(TId id) where TCache : class;

        /// <summary>
        /// Reloads all items for a specific cache type, typically clearing the region first.
        /// </summary>
        Task ReloadCacheForType(Type cacheType);

        /// <summary>
        /// Reloads all items for a specific cache type (identified by TCache), typically clearing the region first.
        /// </summary>
        Task ReloadCacheForType<TCache>() where TCache : class;

        /// <summary>
        /// Updates cache entries for a specific list of IDs.
        /// Fetches the latest DB state for each ID and updates the corresponding cache entry.
        /// </summary>
        Task UpdateCacheForItems<TId>(Type cacheType, IEnumerable<TId> ids);

        /// <summary>
        /// Updates cache entries for a specific list of IDs (identified by TCache).
        /// Fetches the latest DB state for each ID and updates the corresponding cache entry.
        /// </summary>
        Task UpdateCacheForItems<TCache, TId>(IEnumerable<TId> ids) where TCache : class;

        /// <summary>
        /// Updates cache entries for a specific list of IDs (objects).
        /// Determines the ID type from the cacheType and calls the generic version.
        /// </summary>
        Task UpdateCacheForItems(Type cacheType, IEnumerable<object> ids);

        // Note: ReloadItems (from ReloadFromDbModels) is less commonly needed directly by consumers,
        // keeping it off the public interface for now unless specifically required.
        // Task ReloadItems(Type cacheType, IEnumerable<object> dbModels);

        // REMOVED Custom mapping registration
    }
} 