using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheManager.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Dino.Infra.Cache
{
    /// <summary>
    /// High-level, type-safe manager for <see cref="ThreadSafeFlexiCache"/>,
    /// providing generic caching operations, events, and region-based clearing.
    /// </summary>
    public class ThreadSafeFlexiCacheManager : IDisposable
    {
        // A default string to use as the region of an item that has no region.
        private readonly ThreadSafeFlexiCache _cache;
        private readonly DynamicCacheOptions _options;

        /// <summary>Raised when a cache item is successfully retrieved.</summary>
        public event EventHandler<CacheEventArgs> OnCacheHit;
        /// <summary>Raised when a cache lookup fails to find an item.</summary>
        public event EventHandler<CacheEventArgs> OnCacheMiss;
        /// <summary>Raised when a cache item is added or updated.</summary>
        public event EventHandler<CacheEventArgs> OnCacheSet;
        /// <summary>Raised when a cache item is removed.</summary>
        public event EventHandler<CacheEventArgs> OnCacheRemove;

        /// <summary>
        /// Initializes a new instance of <see cref="ThreadSafeFlexiCacheManager"/> with the specified options.
        /// </summary>
        /// <param name="options">Dynamic cache configuration options.</param>
        public ThreadSafeFlexiCacheManager(DynamicCacheOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cache = new ThreadSafeFlexiCache(options);
        }

        /// <summary>
        /// Composes the cache key for a given type and identifier.
        /// </summary>
        private string GetCacheKey<T, TId>(TId id)
        {
            var typeName = _options.GetTypeOptions(typeof(T))?.CustomKeyPrefix ?? typeof(T).Name;
            return $"{typeName}_type_{id}";
        }

        /// <summary>
        /// Gets the region name for a given type.
        /// </summary>
        private string GetRegion<T>()
            => _options.GetTypeOptions(typeof(T))?.CustomKeyPrefix ?? typeof(T).Name;

        /// <summary>
        /// Retrieves an item by its identifier, returning default if not found.
        /// </summary>
        /// <typeparam name="T">Type of the cached item.</typeparam>
        /// <typeparam name="TId">Type of the identifier.</typeparam>
        /// <param name="id">The identifier.</param>
        /// <returns>The cached item or default.</returns>
        public T Get<T, TId>(TId id)
        {
            var key = GetCacheKey<T, TId>(id);
            var region = GetRegion<T>();
            var value = _cache.Get<T>(key, region);
            Raise(value != null ? OnCacheHit : OnCacheMiss, key, value);
            return value;
        }

        /// <summary>
        /// Tries to retrieve an item by its identifier.
        /// </summary>
        /// <typeparam name="T">Type of the cached item.</typeparam>
        /// <typeparam name="TId">Type of the identifier.</typeparam>
        /// <param name="id">The identifier.</param>
        /// <param name="value">Out parameter for the retrieved value.</param>
        /// <returns>True if found, otherwise false.</returns>
        public bool TryGetValue<T, TId>(TId id, out T value)
        {
            var key = GetCacheKey<T, TId>(id);
            var region = GetRegion<T>();
            if (_cache.TryGetValue<T>(key, out T result, region))
            {
                value = result;
                Raise(OnCacheHit, key, value);
                return true;
            }
            value = default;
            Raise(OnCacheMiss, key);
            return false;
        }

        /// <summary>
        /// Inserts or updates a cache entry with optional locking.
        /// </summary>
        /// <typeparam name="T">Type of the cached item.</typeparam>
        /// <typeparam name="TId">Type of the identifier.</typeparam>
        /// <param name="id">The identifier.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="options">Cache entry expiration options.</param>
        /// <param name="ignoreLock">If true, skips key-level locking.</param>
        public void Set<T, TId>(TId id, T value, MemoryCacheEntryOptions options, bool ignoreLock = false)
        {
            var key = GetCacheKey<T, TId>(id);
            var region = GetRegion<T>();
            var exp = options?.AbsoluteExpirationRelativeToNow ?? _options.GetTypeOptions(typeof(T))?.Expiration ?? _options.DefaultExpiration;
            _cache.Set<T>(key, value, exp, ExpirationMode.Absolute, ignoreLock, region);
            Raise(OnCacheSet, key, value);
        }

        public void SetByKeyOnly<T>(string key, T value, MemoryCacheEntryOptions options, bool ignoreLock = false)
        {
            var exp = options?.AbsoluteExpirationRelativeToNow ?? _options.GetTypeOptions(typeof(T))?.Expiration ?? _options.DefaultExpiration;
            _cache.Set<T>(key, value, exp, ExpirationMode.Absolute, ignoreLock, null);
            Raise(OnCacheSet, key, value);
        }

        /// <summary>
        /// Retrieves an item or creates it synchronously if missing.
        /// </summary>
        /// <typeparam name="T">Type of the cached item.</typeparam>
        /// <typeparam name="TId">Type of the identifier.</typeparam>
        /// <param name="id">The identifier.</param>
        /// <param name="factory">Factory to generate the item if missing.</param>
        /// <param name="options">Cache entry expiration options.</param>
        /// <returns>The existing or newly cached item.</returns>
        public T GetOrCreate<T, TId>(TId id, Func<ICacheEntry, T> factory, MemoryCacheEntryOptions options)
        {
            var key = GetCacheKey<T, TId>(id);

            return GetOrCreateByKeyOnly<T>(key, factory, options);
        }

        public T GetOrCreateByKeyOnly<T>(string key, Func<ICacheEntry, T> factory, MemoryCacheEntryOptions options)
        {
            var region = GetRegion<T>();
            var exp = options?.AbsoluteExpirationRelativeToNow
                      ?? _options.GetTypeOptions(typeof(T))?.Expiration
                      ?? _options.DefaultExpiration;
            
            var result = _cache.GetOrCreate<T>(key, factory, exp, ExpirationMode.Absolute, region);
            Raise(result != null ? OnCacheHit : OnCacheMiss, key, result);
            return result;
        }

        /// <summary>
        /// Retrieves an item or creates it asynchronously if missing.
        /// </summary>
        /// <typeparam name="T">Type of the cached item.</typeparam>
        /// <typeparam name="TId">Type of the identifier.</typeparam>
        /// <param name="id">The identifier.</param>
        /// <param name="factory">Async factory to generate the item if missing.</param>
        /// <param name="options">Cache entry expiration options.</param>
        /// <returns>The existing or newly cached item.</returns>
        public async Task<T> GetOrCreateAsync<T, TId>(TId id, Func<ICacheEntry, Task<T>> factory, MemoryCacheEntryOptions options)
        {
            var key = GetCacheKey<T, TId>(id);
            var region = GetRegion<T>();
            var exp = options?.AbsoluteExpirationRelativeToNow
                      ?? _options.GetTypeOptions(typeof(T))?.Expiration
                      ?? _options.DefaultExpiration;
            
            var result = await _cache.GetOrCreateAsync<T>(key, factory, exp, ExpirationMode.Absolute, region);
            Raise(result != null ? OnCacheHit : OnCacheMiss, key, result);
            return result;
        }

        public async Task<T> GetOrCreateByKeyOnlyAsync<T>(string key, Func<ICacheEntry, Task<T>> factory, MemoryCacheEntryOptions options)
        {
            var exp = options?.AbsoluteExpirationRelativeToNow
                      ?? _options.GetTypeOptions(typeof(T))?.Expiration
                      ?? _options.DefaultExpiration;
            
            var result = await _cache.GetOrCreateAsync<T>(key, factory, exp, ExpirationMode.Absolute, null);
            Raise(result != null ? OnCacheHit : OnCacheMiss, key, result);
            return result;
        }

        /// <summary>
        /// Retrieves or creates multiple items in bulk.
        /// </summary>
        /// <typeparam name="T">Type of the cached items.</typeparam>
        /// <typeparam name="TId">Type of the identifiers.</typeparam>
        /// <param name="ids">List of identifiers.</param>
        /// <param name="factory">Factory to load missing items.</param>
        /// <param name="options">Cache entry expiration options.</param>
        /// <returns>Dictionary of all requested items.</returns>
        public async Task<Dictionary<TId, T>> GetOrCreateMultipleAsync<T, TId>(
            List<TId> ids,
            Func<List<TId>, Task<Dictionary<TId, T>>> factory,
            MemoryCacheEntryOptions options)
        {
            // Separate existing cached items and those missing
            var existing = new Dictionary<TId, T>();
            var missing = new Dictionary<TId, string>();
            var region = GetRegion<T>();

            foreach (var id in ids)
            {
                var cacheKey = GetCacheKey<T, TId>(id);
                if (_cache.TryGetValue<T>(cacheKey, out T entity, region))
                {
                    existing[id] = entity;
                }
                else
                {
                    missing[id] = cacheKey;
                }
            }

            if (missing.Count > 0)
            {
                // Lock missing keys
                var waiters = new List<Task>();
                foreach (var kv in missing)
                {
                    var waiter = _cache.LockKeyIfFree(kv.Value);
                    if (waiter != null)
                        waiters.Add(waiter);
                }

                if (waiters.Count > 0)
                    await Task.WhenAll(waiters);

                try
                {
                    // Check again after acquiring all locks
                    var keysToLoad = new List<TId>();
                    foreach (var kv in missing.ToList())
                    {
                        if (_cache.TryGetValue<T>(kv.Value, out T entity, region))
                        {
                            existing[kv.Key] = entity;
                            missing.Remove(kv.Key);
                        }
                        else
                        {
                            keysToLoad.Add(kv.Key);
                        }
                    }

                    if (keysToLoad.Count > 0)
                    {
                        // Bulk load missing values
                        var loaded = await factory(keysToLoad);
                        foreach (var kv in loaded)
                        {
                            var cacheKey = missing[kv.Key];
                            // Ignore lock as it's already acquired
                            var exp = options?.AbsoluteExpirationRelativeToNow
                                      ?? _options.GetTypeOptions(typeof(T))?.Expiration
                                      ?? _options.DefaultExpiration;
                            _cache.Set<T>(cacheKey, kv.Value, exp, ExpirationMode.Absolute, true, region);
                            existing[kv.Key] = kv.Value;
                        }
                    }
                }
                finally
                {
                    // Release locks
                    foreach (var kv in missing)
                        _cache.ReleaseKey(kv.Value);
                }
            }

            return existing;
        }

        /// <summary>Removes an item.</summary>
        public void Remove<T, TId>(TId id)
        {
            var key = GetCacheKey<T, TId>(id);
            var region = GetRegion<T>();
            _cache.Remove<T>(key, false, region);
            Raise(OnCacheRemove, key);
        }

        public void RemoveByKeyOnly(string key)
        {
            _cache.Remove(key, false, null);
            Raise(OnCacheRemove, key);
        }

        /// <summary>Clears all entries for a specific type region.</summary>
        public void ClearRegion<T>()
        {
            var region = GetRegion<T>();
            _cache.ClearRegion(region);
        }

        /// <summary>Clears all cache entries across all regions.</summary>
        public void Clear() => _cache.Clear();

        /// <summary>Disposes the underlying cache.</summary>
        public void Dispose() => _cache.Dispose();

        /// <summary>Safely raises an event handler.</summary>
        private void Raise(EventHandler<CacheEventArgs> handler, string key, object value = null)
        {
            handler?.Invoke(this, new CacheEventArgs(key, value));
        }
    }

    /// <summary>Configuration options for dynamic caching.</summary>
    public class DynamicCacheOptions
    {
        public bool UseRedis { get; set; }
        public string RedisHost { get; set; }
        public int RedisPort { get; set; } = 6379;
        public string RedisPassword { get; set; }
        public int RedisDatabase { get; set; } = 0;
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
        public Dictionary<Type, TypeCacheOptions> TypeOptions { get; set; } = new Dictionary<Type, TypeCacheOptions>();
        public TypeCacheOptions GetTypeOptions(Type type)
            => TypeOptions.TryGetValue(type, out var opts) ? opts : null;
    }

    /// <summary>Per-type cache configuration: key prefix and expiration override.</summary>
    public class TypeCacheOptions
    {
        public string CustomKeyPrefix { get; set; }
        public TimeSpan? Expiration { get; set; }
    }

    /// <summary>Event arguments for cache operations.</summary>
    public class CacheEventArgs : EventArgs
    {
        public string Key { get; }
        public object Value { get; }
        public DateTime Timestamp { get; }
        public CacheEventArgs(string key, object value = null)
        {
            Key = key;
            Value = value;
            Timestamp = DateTime.UtcNow;
        }
    }
}
