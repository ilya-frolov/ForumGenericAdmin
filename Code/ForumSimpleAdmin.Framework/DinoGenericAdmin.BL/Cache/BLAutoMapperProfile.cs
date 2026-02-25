using AutoMapper;
using System;

namespace DinoGenericAdmin.BL.Cache
{
    public class BLAutoMapperProfile : Profile
    {
        private readonly IServiceProvider _serviceProvider;
        private static Func<string, string> _getFullUploadsPathFunc;

        public BLAutoMapperProfile(IServiceProvider serviceProvider, Func<string, string> getFullUploadsPathFunc)
        {
            _serviceProvider = serviceProvider;
            _getFullUploadsPathFunc = getFullUploadsPathFunc;

            // Add mappings for your BL models
            // Example:
            // CreateMap<Service, ServiceCacheModel>();
        }

        public static string GetFullUploadsPath(string path)
        {
            return _getFullUploadsPathFunc(path);
        }
    }
} 