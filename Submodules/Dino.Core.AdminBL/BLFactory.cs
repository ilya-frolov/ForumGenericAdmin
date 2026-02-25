using Microsoft.Extensions.Configuration;
using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dino.Core.AdminBL.Contracts;
using Dino.Core.AdminBL.Cache;
using Dino.Core.AdminBL.Data;

namespace Dino.Core.AdminBL
{
    public class BLFactory<TDbContext, TBlConfig, TCacheManager>
        where TDbContext : BaseDbContext<TDbContext>
        where TBlConfig : BaseBlConfig
        where TCacheManager : BaseDinoCacheManager<TDbContext, TBlConfig, TCacheManager>
    {
        private readonly IConfiguration _config;
        private readonly TBlConfig _blConfig;
        private readonly IServiceProvider _serviceProvider;
        private readonly TCacheManager _cacheManager;
        private readonly IMapper _mapper;
        private TDbContext _context = null;

        public BLFactory(IConfiguration config, IOptions<TBlConfig> blConfig, IServiceProvider serviceProvider, IMapper mapper)
        {
            _blConfig = blConfig.Value;
            _config = config;
            _serviceProvider = serviceProvider;
            _mapper = mapper;
            _cacheManager = serviceProvider.GetService<TCacheManager>();
        }

        public void ResetContext()
        {
            _context = GetNewContext();
        }

        public TDbContext GetNewContext()
        {
            return (TDbContext)Activator.CreateInstance(typeof(TDbContext), _config);
        }

        public T GetBL<T>(bool forceNewContext = false) where T : BaseBL<TDbContext, TBlConfig, TCacheManager>
        {
            if (_context == null)
            {
                _context = GetNewContext();
            }

            var context = _context;
            if (forceNewContext)
            {
                context = GetNewContext();
            }

            return (T)Activator.CreateInstance(typeof(T), this, context, _mapper);
        }

        internal TBlConfig GetConfig()
        {
            return _blConfig;
        }

        internal TCacheManager GetCacheManager()
        {
            return _cacheManager;
        }

        internal T GetServiceInternal<T>()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                return scope.ServiceProvider.GetService<T>();
            }
        }

        public T GetService<T>()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                return scope.ServiceProvider.GetService<T>();
            }
        }

        internal ILogger GetLogger(Type type)
        {
            MethodInfo getServiceMethod = this.GetType().GetMethod("GetServiceInternal", BindingFlags.Instance | BindingFlags.NonPublic);

            // Construct the generic version of the GetService method with the correct type argument.
            var genericLoggerType = typeof(ILogger<>).MakeGenericType(new[] { type });
            MethodInfo getServiceGenericMethod = getServiceMethod.MakeGenericMethod(genericLoggerType);

            // Call the constructed generic GetService method with the type argument.
            return (ILogger)getServiceGenericMethod.Invoke(this, null);
        }
    }
} 