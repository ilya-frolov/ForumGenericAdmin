using System.Threading.Tasks;

namespace Dino.Infra.LockManager
{
    public interface ILockManager
    {
        Task<ILockManagerLock> AcquireLock<T>(object id, int maxRetries = 1);

        Task<ILockManagerLock> AcquireLock(string lockKey, int maxRetries = 1);
    }
}