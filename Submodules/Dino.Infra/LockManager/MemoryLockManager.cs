using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Dino.Infra.LockManager
{
    public class MemoryLockManager : ILockManager
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public async Task<ILockManagerLock> AcquireLock<T>(object id, int maxRetries = 1)
        {
            var lockKey = GetLockKey<T>(id);
            return await AcquireLock(lockKey, maxRetries);
        }

        public async Task<ILockManagerLock> AcquireLock(string lockKey, int maxRetries = 1)
        {
            var @lock = _locks.GetOrAdd(lockKey, new SemaphoreSlim(1));

            // Try to acquire the lock
            for (var i = 0; i < maxRetries; i++)
            {
                var locked = await @lock.WaitAsync(10);
                if (locked)
                {
                    return new MemoryLockManagerLock(@lock);
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            return null;
        }

        private string GetLockKey<T>(object id)
        {
            return $"{typeof(T).Name}_{id}";
        }
    }
}