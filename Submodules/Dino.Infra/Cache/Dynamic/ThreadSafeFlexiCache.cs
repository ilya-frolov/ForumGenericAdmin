using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Dino.Common.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace Dino.Infra.Cache
{
    /// <summary>
    /// Thread-safe cache manager with optional Redis/memory handles, region support,
    /// key-level locking, and aggregated statistics.
    /// </summary>
    public class ThreadSafeFlexiCache : IDisposable
    {
        private readonly ICacheManager<object> _cache;
        private readonly ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();
        private readonly TimeSpan _defaultExpiration;

        /// <summary>
        /// Initializes a new instance of <see cref="ThreadSafeFlexiCache"/>.
        /// </summary>
        /// <param name="options">Configuration options for cache handles and expiration.</param>
        public ThreadSafeFlexiCache(DynamicCacheOptions options)
        {
            _defaultExpiration = options.DefaultExpiration;
            _cache = CacheFactory.Build<object>(cfg =>
            {
                if (options.UseRedis)
                {
                    cfg.WithRedisConfiguration("redis", r => r
                            .WithAllowAdmin()
                            .WithDatabase(options.RedisDatabase)
                            .WithEndpoint(options.RedisHost, options.RedisPort)
                            .WithPassword(options.RedisPassword))
                       .WithRedisCacheHandle("redis", true)
                       .WithExpiration(ExpirationMode.None, TimeSpan.Zero);
                }
                cfg.WithMicrosoftMemoryCacheHandle("memory")
                   .WithExpiration(ExpirationMode.Absolute, _defaultExpiration);
            });
        }

        /// <summary>
        /// Checks whether a cache entry exists in the given region (or default).
        /// </summary>
        private bool Exists(object key, string region)
            => string.IsNullOrWhiteSpace(region)
               ? _cache.Exists(key.ToString())
               : _cache.Exists(key.ToString(), region);

        /// <summary>
        /// Internal get-or-create logic with locking and factory invocation.
        /// </summary>
        private async Task<T> GetOrCreateInternal<T>(
            object key,
            string region,
            Func<ICacheEntry, Task<T>> factory,
            TimeSpan? expiration,
            ExpirationMode mode)
        {
            if (!Exists(key, region))
            {
                var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                await keyLock.WaitAsync();
                try
                {
                    if (!Exists(key, region))
                    {
                        using (var tempCache = new MemoryCache(new MemoryCacheOptions()))
                        {
                            var entry = tempCache.CreateEntry(key);
                            var value = await factory(entry);

                            // Cache only if it's not null.
                            if (value != null)
                            {
                                var cacheItem = CreateCacheItem(key, value, expiration ?? _defaultExpiration, mode,
                                    region);
                                _cache.Put(cacheItem);
                            }

                            return value;
                        }
                    }
                }
                finally
                {
                    if (keyLock.CurrentCount == 0)
                        keyLock.Release();
                }
            }

            T item;
            if (region.IsNotNullOrEmpty())
            {
                item = (T)_cache.Get(key.ToString(), region);
            }
            else
            {
                item = (T)_cache.Get(key.ToString());
            }

            return item;
        }

        private CacheItem<object> CreateCacheItem<T>(
            object key,
            T value,
            TimeSpan? expiration,
            ExpirationMode mode,
            string region = null)
        {
            if (region.IsNotNullOrEmpty())
            {
                return new CacheItem<object>(key.ToString(), region, value, mode, expiration ?? _defaultExpiration);
            }
            else
            {
                return new CacheItem<object>(key.ToString(), value, mode, expiration ?? _defaultExpiration);
            }
        }

        /// <summary>
        /// Retrieves an item or generates and caches it if missing.
        /// </summary>
        public T GetOrCreate<T>(
            object key,
            Func<ICacheEntry, T> factory,
            TimeSpan? expiration = null,
            ExpirationMode mode = ExpirationMode.Absolute,
            string region = null)
            => GetOrCreateInternal<T>(key, region, entry => Task.FromResult(factory(entry)), expiration, mode)
               .GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronous version of GetOrCreate.
        /// </summary>
        public Task<T> GetOrCreateAsync<T>(
            object key,
            Func<ICacheEntry, Task<T>> factory,
            TimeSpan? expiration = null,
            ExpirationMode mode = ExpirationMode.Absolute,
            string region = null)
            => GetOrCreateInternal<T>(key, region, factory, expiration, mode);

        /// <summary>
        /// Inserts or updates a cache entry with optional locking.
        /// </summary>
        public void Set<T>(
            object key,
            T value,
            TimeSpan? expiration = null,
            ExpirationMode mode = ExpirationMode.Absolute,
            bool ignoreLock = false,
            string region = null)
        {
            var cacheItem = CreateCacheItem(key, value, expiration, mode, region);

            if (ignoreLock)
            {
                _cache.Put(cacheItem);
                return;
            }

            var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            keyLock.Wait();
            try
            {
                _cache.Put(cacheItem);
            }
            finally
            {
                if (keyLock.CurrentCount == 0)
                    keyLock.Release();
            }
        }

        /// <summary>
        /// Retrieves an entry; returns null if not found.
        /// </summary>
        public T Get<T>(object key, string region = null)
        {
            object result;
            if (region.IsNotNullOrEmpty())
            {
                result = _cache.Get(key.ToString(), region);
            }
            else
            {
                result = _cache.Get(key.ToString());
            }

            return result != null ? (T)result : default;
        }

        /// <summary>
        /// Tries to get a value; indicates success via return.
        /// </summary>
        public bool TryGetValue<T>(object key, out T value, string region = null)
        {
            object result;
            if (region.IsNotNullOrEmpty())
            {
                result = _cache.Get(key.ToString(), region);
            }
            else
            {
                result = _cache.Get(key.ToString());
            }

            if (result != null)
            {
                value = (T)result;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Removes a cache entry with optional locking.
        /// </summary>
        public void Remove<T>(object key, bool ignoreLock = false, string region = null)
        {
            Remove(key, ignoreLock, region);
        }

        public void Remove(object key, bool ignoreLock = false, string region = null)
        {
            if (!Exists(key, region))
                return;

            if (ignoreLock)
            {
                if (region.IsNotNullOrEmpty())
                {
                    _cache.Remove(key.ToString(), region);
                }
                else
                {
                    _cache.Remove(key.ToString());
                }
                return;
            }

            var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            keyLock.Wait();
            try
            {
                if (region.IsNotNullOrEmpty())
                {
                    _cache.Remove(key.ToString(), region);
                }
                else
                {
                    _cache.Remove(key.ToString());
                }
            }
            finally
            {
                if (keyLock.CurrentCount == 0)
                    keyLock.Release();
            }
        }

        /// <summary>
        /// Clears all entries in the specified region.
        /// </summary>
        public void ClearRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                throw new ArgumentNullException(nameof(region));

            _cache.ClearRegion(region);
        }

        /// <summary>
        /// Clears whole cache and resets locks.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _locks.Clear();
        }

        /// <summary>
        /// Aggregates statistics across all cache handles.
        /// </summary>
        public CacheStats GetStats()
        {
            var stats = new CacheStats();
            foreach (var handle in _cache.CacheHandles)
            {
                if (handle.Stats != null)
                {
                    stats.Hits += (int)handle.Stats.GetStatistic(CacheStatsCounterType.Hits);
                    stats.Misses += (int)handle.Stats.GetStatistic(CacheStatsCounterType.Misses);
                    stats.Sets += (int)handle.Stats.GetStatistic(CacheStatsCounterType.AddCalls);
                    stats.Removes += (int)handle.Stats.GetStatistic(CacheStatsCounterType.RemoveCalls);
                    stats.SavedItems += (int)handle.Stats.GetStatistic(CacheStatsCounterType.Items);
                }
            }
            return stats;
        }

        /// <summary>
        /// Locks a key if free; otherwise waits asynchronously.
        /// This is an internal method used by the cache manager.
        /// </summary>
        internal Task LockKeyIfFree(object key)
        {
            var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            bool wasLocked = keyLock.Wait(0);
            
            // Return null if the lock was acquired immediately (consistent with ThreadSafeMemoryCache)
            // Otherwise return a task to wait for the lock
            if (wasLocked)
                return null;
            else
                return keyLock.WaitAsync();
        }

        /// <summary>
        /// Releases a previously acquired lock on the key.
        /// This is an internal method used by the cache manager.
        /// </summary>
        internal void ReleaseKey(object key)
        {
            if (_locks.TryGetValue(key, out var keyLock) && keyLock.CurrentCount == 0)
            {
                try
                {
                    keyLock.Release();
                }
                catch (SemaphoreFullException)
                {
                    // Ignore this exception. We don't know why this happens on some occasions and this doesn't really change too much for us.
                }
            }
        }

        /// <summary>
        /// Disposes the cache handles and clears locks.
        /// </summary>
        public void Dispose()
        {
            _cache.Dispose();
            _locks.Clear();
        }
    }

    /// <summary>
    /// Aggregated statistics for cache operations.
    /// </summary>
    public class CacheStats
    {
        public int Hits { get; set; }
        public int Misses { get; set; }
        public int Sets { get; set; }
        public int Removes { get; set; }
        public int SavedItems { get; set; }
        public int Total => Hits + Misses;
        public double HitRatio => Total > 0 ? (double)Hits / Total : 0;
    }
}
