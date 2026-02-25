using System.Collections.Concurrent;
using System.Reflection;
using Dino.Core.AdminBL.Data;
using Dino.Core.AdminBL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dino.Core.AdminBL.Settings
{
    public class SettingsProvider : ISettingsProvider
    {
        private readonly ILogger<SettingsProvider> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<Type, IAdminBaseSettings> _cache = new();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IAdminModelMapper _adminModelMapper;
        
        public event Action<Type> SettingsChanged;

        public SettingsProvider(
            ILogger<SettingsProvider> logger, 
            IServiceProvider serviceProvider,
            IAdminModelMapper adminModelMapper)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _adminModelMapper = adminModelMapper;
        }
        
        public async Task<T> GetAsync<T>() where T : IAdminBaseSettings
        {
            var requestedType = typeof(T);
            
            // Check if this exact type is already cached
            if (_cache.TryGetValue(requestedType, out var cachedSettings))
            {
                return (T)cachedSettings;
            }
            
            Type concreteType = requestedType;
            
            // If the requested type is abstract or interface, find concrete implementation
            if (requestedType.IsAbstract || requestedType.IsInterface)
            {
                concreteType = FindConcreteImplementation(requestedType);
                if (concreteType == null)
                {
                    _logger.LogError($"No concrete implementation found for abstract settings type {requestedType.Name}");
                    return default;
                }
            }
            
            // Check if the concrete implementation is cached
            IAdminBaseSettings concreteSettings;
            if (_cache.TryGetValue(concreteType, out var existingConcreteSettings))
            {
                concreteSettings = existingConcreteSettings;
            }
            else
            {
                var dbContext = GetDbContext();

                // Load concrete settings from database
                var setting = await LoadSettingsFromDbAsync(concreteType, dbContext);
                if (setting == null)
                {
                    return default;
                }

                // Convert the DB entity to the admin model using the IAdminModelMapper
                concreteSettings = (IAdminBaseSettings)_adminModelMapper.ToAdminModelFromTypes(setting, concreteType, typeof(Setting), dbContext);
                if (concreteSettings == null)
                {
                    return default;
                }
                
                // Cache the concrete implementation
                _cache[concreteType] = concreteSettings;
            }
            
            // If requested type is not the concrete type, create and cache a copy cast to the requested type
            if (requestedType != concreteType)
            {
                if (requestedType.IsAssignableFrom(concreteType))
                {
                    // The requested type is a base class/interface, so we can just cast and cache it
                    _cache[requestedType] = concreteSettings;
                    return (T)concreteSettings;
                }
                else
                {
                    _logger.LogError($"Cannot convert concrete type {concreteType.Name} to requested type {requestedType.Name}");
                    return default;
                }
            }
            
            return (T)concreteSettings;
        }
        
        public async Task ReloadAsync<T>() where T : IAdminBaseSettings
        {
            var requestedType = typeof(T);
            
            // Remove the requested type from cache
            _cache.TryRemove(requestedType, out _);
            
            // If it's an abstract type or interface, find and reload all implementations
            if (requestedType.IsAbstract || requestedType.IsInterface)
            {
                // Find all concrete implementations of the requested type
                var implementationTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => {
                        try {
                            return a.GetTypes();
                        }
                        catch (ReflectionTypeLoadException) {
                            return Array.Empty<Type>();
                        }
                    })
                    .Where(t => t.IsClass && !t.IsAbstract && requestedType.IsAssignableFrom(t))
                    .ToList();
                
                foreach (var type in implementationTypes)
                {
                    // Remove from cache and reload
                    _cache.TryRemove(type, out _);
                    await LoadAndCacheSettingsAsync(type);
                }
            }
            else
            {
                // Concrete type, just reload it
                await LoadAndCacheSettingsAsync(requestedType);
            }
            
            SettingsChanged?.Invoke(requestedType);
        }
        
        public async Task ReloadAllAsync()
        {
            await InitializeAsync();
        }
        
        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing settings provider");
            
            try
            {
                // Use semaphore to prevent concurrent initialization
                await _semaphore.WaitAsync();
                
                try
                {
                    // Clear any existing cache
                    _cache.Clear();
                    
                    // Find all concrete settings types
                    var settingsTypes = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => {
                            try {
                                return a.GetTypes();
                            }
                            catch (ReflectionTypeLoadException) {
                                return Array.Empty<Type>();
                            }
                        })
                        .Where(t => t.IsClass && !t.IsAbstract && 
                               typeof(IAdminBaseSettings).IsAssignableFrom(t))
                        .ToList();
                    
                    // Load settings in parallel
                    var tasks = settingsTypes.Select(type => LoadAndCacheSettingsAsync(type));
                    await Task.WhenAll(tasks);
                    
                    // Also cache base classes for each concrete implementation
                    foreach (var type in settingsTypes)
                    {
                        if (_cache.TryGetValue(type, out var settings))
                        {
                            // Find all base types and interfaces that are IAdminBaseSettings
                            var baseTypes = GetBaseTypes(type)
                                .Where(t => typeof(IAdminBaseSettings).IsAssignableFrom(t) && 
                                       t != type && 
                                       !_cache.ContainsKey(t))
                                .ToList();
                            
                            // Cache the settings instance for each base type
                            foreach (var baseType in baseTypes)
                            {
                                _cache[baseType] = settings;
                            }
                        }
                    }
                    
                    _logger.LogInformation($"Loaded {_cache.Count} settings to cache");
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing settings provider");
            }
        }
        
        private Type FindConcreteImplementation(Type abstractType)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException) {
                        return Array.Empty<Type>();
                    }
                })
                .FirstOrDefault(t => t.IsClass && !t.IsAbstract && abstractType.IsAssignableFrom(t));
        }
        
        private IEnumerable<Type> GetBaseTypes(Type type)
        {
            // Get base classes
            for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                yield return baseType;
            }
            
            // Get interfaces
            foreach (var interfaceType in type.GetInterfaces())
            {
                yield return interfaceType;
            }
        }

        private BaseAdminDbContext GetDbContext()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BaseAdminDbContext>();

            return dbContext;
        }

        private async Task<Setting> LoadSettingsFromDbAsync(Type settingsType, BaseAdminDbContext dbContext)
        {
            try
            {
                var setting = await dbContext.Settings.FirstOrDefaultAsync(s => s.ClassName == settingsType.Name);
                if (setting == null)
                {
                    _logger.LogWarning($"Settings not found for type {settingsType.Name}");
                    return null;
                }
                
                return setting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading settings for type {settingsType.Name}");
                return null;
            }
        }
        
        private async Task LoadAndCacheSettingsAsync(Type settingsType)
        {
            var dbContext = GetDbContext();
            var setting = await LoadSettingsFromDbAsync(settingsType, dbContext);
            if (setting != null)
            {
                var adminSettings = (IAdminBaseSettings)_adminModelMapper.ToAdminModelFromTypes(setting, settingsType, typeof(Setting), dbContext);
                if (adminSettings != null)
                {
                    _cache[settingsType] = adminSettings;
                }
            }
        }
    }
} 