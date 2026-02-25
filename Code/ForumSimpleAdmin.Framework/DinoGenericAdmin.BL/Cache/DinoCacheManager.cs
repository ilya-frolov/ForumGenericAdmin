using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using DinoGenericAdmin.BL.Models;
using System.Threading.Tasks;
using Dino.Core.AdminBL;
using DinoGenericAdmin.BL.Contracts;
using Dino.Core.AdminBL.Cache;
using DinoGenericAdmin.BL.Data;

namespace DinoGenericAdmin.BL.Cache
{
    public class DinoCacheManager : BaseDinoCacheManager<MainDbContext, BlConfig, DinoCacheManager>
    {
        public DinoCacheManager(IConfiguration config, IMapper mapper, IOptions<BlConfig> blConfig, IServiceProvider serviceProvider)
            : base(config, mapper, blConfig, serviceProvider)
        {
            //Subscribe to cache events
            //_cacheManager.OnCacheHit += (sender, args) =>
            //{
            //    Console.WriteLine($"Cache hit for {args.Key} at {args.Timestamp}");
            //};

            // Get cache statistics. NOT FULLY IMPLEMENTED.
            //var stats = _cacheManager.GetStats("MyType_123");
            //Console.WriteLine($"Hit ratio: {stats.HitRatio:P}");
        }
    }
} 