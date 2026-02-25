using AutoMapper;
using Dino.CoreMvc.Admin.AutoMapper;
using Dino.CoreMvc.Admin.FieldTypePlugins;
using Dino.CoreMvc.Admin.Logic.Converters;
using Dino.CoreMvc.Admin.Logic.Helpers;
using DinoGenericAdmin.Api.Models;
using DinoGenericAdmin.BL.Contracts;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DinoGenericAdmin.Api.Logic.Converters
{
    public class MapperProfile : AdminBaseMapperProfile
    {
        private readonly IServiceProvider _serviceProvider;

        public MapperProfile(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // Shared Mappings
            //CreateMap<string, FileContainer>()
            //    .ForMember(dest => dest.Path, opt => opt.MapFrom(src => GetUploadsPath(src)));

            // Initialize DynamicConversionHelpers with the path converter
            DynamicConversionHelpers.Initialize(GetUploadsPath);

            // Register generic type converters for string to strongly-typed conversions
            // This eliminates the need for manual ConvertDynamicToFileCollection calls in every mapping
            // Instead of: .ForMember(dest => dest.Image, opt => opt.MapFrom(src => DynamicConversionHelpers.ConvertDynamicToFileCollection(src.Image)))
            // You can now just: .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.Image))
            CreateMap<object, FileCollectionForClient>().ConvertUsing<ObjectToFileCollectionConverter>();
            CreateMap<object, UrlFieldType>().ConvertUsing<ObjectToUrlFieldConverter>();
			
			CreateMap<string, FileCollectionForClient>().ConvertUsing<ObjectToFileCollectionConverter>();
            CreateMap<string, UrlFieldType>().ConvertUsing<ObjectToUrlFieldConverter>();

        }

        #region Helpers

        private IOptions<ApiConfig> GetApiConfig()
        {
            return _serviceProvider.GetService<IOptions<ApiConfig>>();
        }

        private IOptions<BlConfig> GetBlConfig()
        {
            return _serviceProvider.GetService<IOptions<BlConfig>>();
        }

        protected override string GetUploadsPath(string path)
        {
            return PathHelpers.GetUploadsFullPath(GetApiConfig().Value, GetBlConfig().Value, path);
        }

        public string PublicGetUploadsPath(string path)
        {
            return GetUploadsPath(path);
        }

        protected override void AdminMappings()
        {
        }

        #endregion

        protected override void ClientMappings()
        {
            // Implement your mappings here.

            // Example of how to use the generic object converters:
            // Instead of manual conversion calls, AutoMapper will automatically convert object/dynamic properties
            //
            // BEFORE (manual approach):
            // CreateMap<SomeSource, SomeDestination>()
            //     .ForMember(dest => dest.Image, opt => opt.MapFrom(src => DynamicConversionHelpers.ConvertDynamicToFileCollection(src.Image)))
            //     .ForMember(dest => dest.Url, opt => opt.MapFrom(src => DynamicConversionHelpers.ConvertDynamicToUrlFieldType(src.Url)));
            //
            // AFTER (generic approach - automatic conversion):
            // CreateMap<SomeSource, SomeDestination>()
            //     .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.Image))  // Automatic conversion!
            //     .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.Url));     // Automatic conversion!
        }
    }

    #region Generic Type Converters

    /// <summary>
    /// Generic converter for object to FileCollectionForClient
    /// Eliminates the need for manual ConvertDynamicToFileCollection calls in every mapping
    /// </summary>
    public class ObjectToFileCollectionConverter : ITypeConverter<object, FileCollectionForClient>
    {
        public FileCollectionForClient Convert(object source, FileCollectionForClient destination, ResolutionContext context)
        {
            // Convert object to dynamic and use the existing DynamicConversionHelpers method
            dynamic dynamicSource = source;
            return DynamicConversionHelpers.ConvertDynamicToFileCollection(dynamicSource);
        }
    }

    /// <summary>
    /// Generic converter for object to UrlFieldType
    /// </summary>
    public class ObjectToUrlFieldConverter : ITypeConverter<object, UrlFieldType>
    {
        public UrlFieldType Convert(object source, UrlFieldType destination, ResolutionContext context)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                // Convert object to dynamic for processing
                dynamic dynamicSource = source;
                var urlFieldTypeString = dynamicSource.ToString();
                return JsonConvert.DeserializeObject<UrlFieldType>(urlFieldTypeString);
            }
            catch
            {
                return null;
            }
        }
    }

    #endregion
}