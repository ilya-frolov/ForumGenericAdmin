using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Dino.Common.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace Dino.Infra.Cache
{
    public class ThreadSafeMemoryCache : IDisposable
    {
        private readonly MemoryCache _memoryCache;
        private readonly ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();

        public ThreadSafeMemoryCache(MemoryCacheOptions options)
        {
            _memoryCache = new MemoryCache(options);
        }

        public void Set(object key, object value, MemoryCacheEntryOptions options, bool ignoreLock = false)
        {
            // Make sure we need to lock.
            if (!ignoreLock)
            {
                var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                keyLock.Wait();

                try
                {
                    _memoryCache.Set(key, value, options);
                }
                finally
                {
                    if (keyLock.CurrentCount == 0)
                    {
                        try
                        {
                            keyLock.Release();
                        }
                        catch (SemaphoreFullException e)
                        {
                            // Ignore this exception. We don't know why this happens on some occasions and this doesn't really change too much for us.
                        }
                    }
                }
            }
            else
            {
                // Just set it.
                _memoryCache.Set(key, value, options);
            }
        }

        public object Get(object key)
        {
            if (_memoryCache.TryGetValue(key, out object value))
            {
                return value;
            }
            return null;
        }

        public bool TryGetValue(object key, out object value)
        {
            return _memoryCache.TryGetValue(key, out value);
        }

        public TItem GetOrCreate<TItem>(object key, Func<ICacheEntry, TItem> factory)
        {
            if (!_memoryCache.TryGetValue(key, out TItem result))
            {
                var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                keyLock.Wait();
                try
                {
                    if (!_memoryCache.TryGetValue(key, out result))
                    {
                        result = _memoryCache.GetOrCreate(key, factory);
                    }
                }
                finally
                {
                    if (keyLock.CurrentCount == 0)
                    {
                        try
                        {
                            keyLock.Release();
                        }
                        catch (SemaphoreFullException e)
                        {
                            // Ignore this exception. We don't know why this happens on some occasions and this doesn't really change too much for us.
                        }
                    }
                }
            }
            return result;
        }

        public async Task<TItem> GetOrCreateAsync<TItem>(object key, Func<ICacheEntry, Task<TItem>> factory, CancellationToken token = default)
        {
            if (!_memoryCache.TryGetValue(key, out TItem result))
            {
                var rttt = new ConcurrentDictionary<int, string>();

                var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                await keyLock.WaitAsync(token);
                try
                {
                    if (!_memoryCache.TryGetValue(key, out result))
                    {
                        result = await _memoryCache.GetOrCreateAsync(key, factory);
                    }
                }
                finally
                {
                    if (keyLock.CurrentCount == 0)
                    {
                        try
                        {
                            keyLock.Release();
                        }
                        catch (SemaphoreFullException e)
                        {
                            // Ignore this exception. We don't know why this happens on some occasions and this doesn't really change too much for us.
                        }
                    }
                }
            }
            return result;
        }

        public void Remove<TItem>(object key)
        {
            if (_memoryCache.TryGetValue(key, out TItem result))
            {
                var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                keyLock.Wait();
                try
                {
                    if (_memoryCache.TryGetValue(key, out result))
                    {
                        _memoryCache.Remove(key);
                    }
                }
                finally
                {
                    if (keyLock.CurrentCount == 0)
                    {
                        try
                        {
                            keyLock.Release();
                        }
                        catch (SemaphoreFullException e)
                        {
                            // Ignore this exception. We don't know why this happens on some occasions and this doesn't really change too much for us.
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Locks a specific key, but only if it's free (and not locked).
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>A task of an already locked key, if it was locked..</returns>
        internal Task LockKeyIfFree(object key)
        {
            bool wasLocked = false;
            var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            wasLocked = keyLock.Wait(0);

            // Lock if it wasn't locked. Otherwise, get a task to wait for it later.
            Task waiter = null;
            if (!wasLocked)
            {
                waiter = keyLock.WaitAsync();
            }

            return waiter;
        }

        internal void ReleaseKey(object key)
        {
            if (_locks.TryGetValue(key, out SemaphoreSlim keyLock) && (keyLock.CurrentCount == 0))
            {
                try
                {
                    keyLock.Release();
                }
                catch (SemaphoreFullException e)
                {
                    // Ignore this exception. We don't know why this happens on some occasions and this doesn't really change too much for us.
                }
            }
        }

        public void Clear()
        {
            _memoryCache.Clear();
            _locks.Clear();
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
            _locks.Clear();
        }
    }
}
