using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dino.Common.Helpers;
using System.Data;
using System.Threading;

namespace Dino.Infra.Cache
{
    public class ThreadSafeCacheManager : IDisposable
    {
        private readonly ThreadSafeMemoryCache _memoryCache;

        public ThreadSafeCacheManager(MemoryCacheOptions options)
        {
            _memoryCache = new ThreadSafeMemoryCache(options);
        }

        /// <summary>
        /// Gets the key so save in the memory cache. This key is a combination of the type of the entity and its ID.
        /// For example: Item with ID=3, will be saved as Item_3
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="IdType">The entity's ID type.</typeparam>
        /// <param name="key">The entity's real ID.</param>
        /// <returns>The memory-cache ID.</returns>
        private string GetMemoryCacheKey<T, IdType>(IdType key)
        {
            return typeof(T).Name + "_" + key.ToString();
        }

        /// <summary>
        /// Gets an entity by it's real key (will be converted to memory-cache key automatically).
        /// Will force GET, and will never create.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="IdType">The entity's ID type.</typeparam>
        /// <param name="key">The entity's key.</param>
        /// <returns>The entity, or NULL if nothing was found.</returns>
        public T Get<T, IdType>(IdType key)
        {
            return (T)_memoryCache.Get(GetMemoryCacheKey<T, IdType>(key));
        }

        /// <summary>
        /// Gets an entity by it's real key (will be converted to memory-cache key automatically).
        /// Will force GET, and will never create.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="IdType">The entity's ID type.</typeparam>
        /// <param name="key">The entity's key (the real one).</param>
        /// <param name="value">The entity's value.</param>
        /// <returns>Did we find the entity.</returns>
        public bool TryGetValue<T, IdType>(IdType key, out T value)
        {
            value = default;
            var cachedValue = _memoryCache.Get(GetMemoryCacheKey<T, IdType>(key));
            if (cachedValue != null)
            {
                value = (T)cachedValue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets a value in the memory-cache.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="IdType">The entity's ID type.</typeparam>
        /// <param name="key">The entity's key (the real one).</param>
        /// <param name="value">The value to save for this key.</param>
        /// <param name="options"></param>
        public void Set<T, IdType>(IdType key, T value, MemoryCacheEntryOptions options, bool ignoreLock = false)
        {
            _memoryCache.Set(GetMemoryCacheKey<T, IdType>(key), value, options, ignoreLock);
        }

        /// <summary>
        /// Gets or create the entity in the cache.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="IdType">The entity's ID type.</typeparam>
        /// <param name="key">The entity's key (the real one).</param>
        /// <param name="factory">The logic to retrieve the entity in case it wasn't loaded to the memory yet.</param>
        /// <param name="options"></param>
        /// <returns>The entity.</returns>
        public T GetOrCreate<T, IdType>(IdType key, Func<T> factory, MemoryCacheEntryOptions options)
        {
            return _memoryCache.GetOrCreate(GetMemoryCacheKey<T, IdType>(key), entry =>
            {
                entry.SetOptions(options);
                return factory();
            });
        }

        /// <summary>
        /// Gets or create the entity in the cache.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="IdType">The entity's ID type.</typeparam>
        /// <param name="key">The entity's key (the real one).</param>
        /// <param name="factory">The logic to retrieve the entity in case it wasn't loaded to the memory yet.</param>
        /// <param name="options"></param>
        /// <returns>The entity.</returns>
        public async Task<T> GetOrCreateAsync<T, IdType>(IdType key, Func<Task<T>> factory, MemoryCacheEntryOptions options)
        {
            return await _memoryCache.GetOrCreateAsync(GetMemoryCacheKey<T, IdType>(key), async entry =>
            {
                entry.SetOptions(options);
                return await factory();
            });
        }

        /// <summary>
        /// Removes the entity in the cache.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="IdType">The entity's ID type.</typeparam>
        /// <param name="key">The entity's key (the real one).</param>
        public void Remove<T, IdType>(IdType key)
        {
            _memoryCache.Remove<T>(GetMemoryCacheKey<T, IdType>(key));
        }


        /// <summary>
        /// Gets or create multiple entities in the cache.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="IdType">The entity's ID type.</typeparam>
        /// <param name="keys">The entities keys (the real ones).</param>
        /// <param name="factory">The logic to retrieve the missing entities in case it wasn't loaded to the memory yet.</param>
        /// <param name="options"></param>
        /// <returns>All the entities whether they were created or already exist in the cache.</returns>
        public async Task<Dictionary<IdType, T>> GetOrCreateMultipleAsync<T, IdType>(List<IdType> keys, Func<List<IdType>, Task<Dictionary<IdType, T>>> factory,
            MemoryCacheEntryOptions options)
        {
            // Get only the missing keys.
            var missingKeysDic = SeparateMissingEntitiesFromList<T, IdType>(keys, out var entitiesDic);
            if (missingKeysDic.Any())
            {
                // Lock the keys. Only keep the ones that aren't already locked (otherwise, it means we're already loading them somewhere).
                var reverseKeyDic = new Dictionary<string, IdType>();
                var missingKeysToLoad = new HashSet<string>();
                var loadingKeys = new HashSet<string>();
                var waitersForLoadingKeys = new List<Task>();
                foreach (var keyPair in missingKeysDic)
                {
                    reverseKeyDic.Add(keyPair.Value, keyPair.Key);

                    var waiter = _memoryCache.LockKeyIfFree(keyPair.Value);
                    if (waiter != null)
                    {
                        loadingKeys.Add(keyPair.Value);
                        waitersForLoadingKeys.Add(waiter);
                    }
                    else
                    {
                        missingKeysToLoad.Add(keyPair.Value);
                    }
                }

                // Load.
                try
                {
                    // Create the missing ones.
                    var models = await factory(missingKeysDic.SelectList(x => x.Key));
                    foreach (var model in models)
                    {
                        var currCacheKey = missingKeysDic[model.Key];
                        Set(model.Key, model.Value, options, true); // Ignore the lock, because we already locked it.
                        entitiesDic.AddIfNotExists(model.Key, model.Value);
                        _memoryCache.ReleaseKey(currCacheKey);
                        missingKeysToLoad.Remove(currCacheKey);
                    }

                    // Wait for all the loading keys, and release them right after.
                    Task.WaitAll(waitersForLoadingKeys.ToArray());
                    foreach (var currKey in loadingKeys)
                    {
                        if (_memoryCache.TryGetValue(currKey, out var currEntity))
                        {
                            entitiesDic.AddIfNotExists(reverseKeyDic[currKey], (T) currEntity);
                        }

                        _memoryCache.ReleaseKey(currKey);
                        loadingKeys.Remove(currKey);
                    }
                }
                finally
                {
                    // Release the keys.
                    missingKeysToLoad.Foreach(x => _memoryCache.ReleaseKey(x));
                    loadingKeys.Foreach(x => _memoryCache.ReleaseKey(x));
                }
            }

            return entitiesDic;
        }


        /// <summary>
        /// Checks a list of keys of a specific type, and returns only the keys that are not loaded to the cache yet.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <typeparam name="IdType">The entity's ID type.</typeparam>
        /// <param name="keys">The keys to check.</param>
        /// <param name="existingEntities">Dictionary of existing entities.</param>
        /// <returns>The keys that are missing (not loaded to the cache yet), as the original key and the memory-cache key.</returns>
        public Dictionary<IdType, string> SeparateMissingEntitiesFromList<T, IdType>(List<IdType> keys, out Dictionary<IdType, T> existingEntities)
        {
            existingEntities = new Dictionary<IdType, T>();
            var missingKeys = new Dictionary<IdType, string>();

            foreach (var key in keys)
            {
                var memoryCacheKey = GetMemoryCacheKey<T, IdType>(key);
                if (TryGetValue(memoryCacheKey, out T entity))
                {
                    existingEntities.AddIfNotExists(key, entity);
                }
                else
                {
                    missingKeys.AddIfNotExists(key, memoryCacheKey);
                }

            }

            return missingKeys;
        }

        public void Clear()
        {
            _memoryCache.Clear();
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
        }
    }
}