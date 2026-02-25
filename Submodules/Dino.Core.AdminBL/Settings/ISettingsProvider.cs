using Dino.Core.AdminBL.Settings;

namespace Dino.Core.AdminBL.Settings
{
    public interface ISettingsProvider
    {
        Task<T> GetAsync<T>() where T : IAdminBaseSettings;
        
        Task ReloadAsync<T>() where T : IAdminBaseSettings;
        
        Task ReloadAllAsync();
        
        Task InitializeAsync();
        
        event Action<Type> SettingsChanged;
    }
} 