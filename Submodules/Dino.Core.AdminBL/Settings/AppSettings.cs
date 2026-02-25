using Dino.Core.AdminBL.Settings;
using System.Threading;

namespace Dino.Core.AdminBL.Settings
{
    /// <summary>
    /// Static accessor for application settings
    /// </summary>
    public static class AppSettings
    {
        private static ISettingsProvider _provider;
        private static readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);
        private static bool _isInitialized = false;

        /// <summary>
        /// Initializes the AppSettings with a settings provider
        /// </summary>
        /// <param name="provider">The settings provider</param>
        public static async Task InitAsync(ISettingsProvider provider)
        {
            await _initSemaphore.WaitAsync();
            try
            {
                _provider = provider;
                await _provider.InitializeAsync();
                _isInitialized = true;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets settings by type
        /// </summary>
        /// <typeparam name="T">Settings type</typeparam>
        /// <returns>Settings instance</returns>
        public static async Task<T> GetAsync<T>() where T : IAdminBaseSettings 
        {
            EnsureInitialized();
            return await _provider.GetAsync<T>();
        }
        
        /// <summary>
        /// Gets settings by type synchronously (uses async method internally)
        /// Only use this when you cannot use async/await
        /// </summary>
        /// <typeparam name="T">Settings type</typeparam>
        /// <returns>Settings instance</returns>
        public static T Get<T>() where T : IAdminBaseSettings
        {
            EnsureInitialized();
            // This is a blocking call but we're keeping it for backward compatibility
            // and convenience in situations where async isn't easily used
            return GetAsync<T>().GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Reloads settings of the specified type
        /// </summary>
        /// <typeparam name="T">Settings type to reload</typeparam>
        public static async Task ReloadAsync<T>() where T : IAdminBaseSettings
        {
            EnsureInitialized();
            await _provider.ReloadAsync<T>();
        }
        
        /// <summary>
        /// Reloads all settings
        /// </summary>
        public static async Task ReloadAllAsync()
        {
            EnsureInitialized();
            await _provider.ReloadAllAsync();
        }
        
        /// <summary>
        /// Registers a callback to be invoked when settings of a specific type are changed
        /// </summary>
        /// <typeparam name="T">Settings type</typeparam>
        /// <param name="callback">Callback to invoke</param>
        public static void OnChanged<T>(Action callback) where T : IAdminBaseSettings
        {
            EnsureInitialized();
            
            _provider.SettingsChanged += (changedType) =>
            {
                if (changedType == typeof(T) || typeof(T).IsAssignableFrom(changedType))
                {
                    callback();
                }
            };
        }
        
        private static void EnsureInitialized()
        {
            if (!_isInitialized || _provider == null)
            {
                throw new InvalidOperationException("AppSettings has not been initialized. Call AppSettings.InitAsync() first.");
            }
        }
    }
} 