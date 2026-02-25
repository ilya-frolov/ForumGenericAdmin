using System.Threading;
using System.Threading.Tasks;

namespace Dino.Infra.LockManager
{
    public class MemoryLockManagerLock : ILockManagerLock
    {
        private SemaphoreSlim _semaphore;

        public MemoryLockManagerLock(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public async Task Release()
        {
            _semaphore?.Release();
            _semaphore = null;
        }

        public void Dispose()
        {
            _semaphore?.Release();
        }
    }
}