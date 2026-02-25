using System;
using System.Threading.Tasks;

namespace Dino.Infra.LockManager
{
    public interface ILockManagerLock : IDisposable
    {
        Task Release();
    }
}