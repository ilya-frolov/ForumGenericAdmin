using AutoMapper;
using Dino.CoreMvc.Admin.AutoMapper;
using Dino.CoreMvc.Admin.FieldTypePlugins;
using Dino.CoreMvc.Admin.Logic.Converters;
using Dino.CoreMvc.Admin.Logic.Helpers;
using DinoGenericAdmin.Api.Models;
using ForumSimpleAdmin.Api.Models;
using ForumSimpleAdmin.BL.Contracts;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ForumSimpleAdmin.Api.Logic.Converters
{
    public class MapperProfile : AdminBaseMapperProfile
    {
        private readonly IServiceProvider _serviceProvider;

        public MapperProfile(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;

            DynamicConversionHelpers.Initialize(GetUploadsPath);
            CreateMap<object, FileCollectionForClient>().ConvertUsing<ObjectToFileCollectionConverter>();
            CreateMap<object, UrlFieldType>().ConvertUsing<ObjectToUrlFieldConverter>();
            CreateMap<string, FileCollectionForClient>().ConvertUsing<ObjectToFileCollectionConverter>();
            CreateMap<string, UrlFieldType>().ConvertUsing<ObjectToUrlFieldConverter>();
        }

        private IOptions<ApiConfig> GetApiConfig()
        {
            IOptions<ApiConfig> value = _serviceProvider.GetService<IOptions<ApiConfig>>()!;
            return value;
        }

        private IOptions<BlConfig> GetBlConfig()
        {
            IOptions<BlConfig> value = _serviceProvider.GetService<IOptions<BlConfig>>()!;
            return value;
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

        protected override void ClientMappings()
        {
        }
    }

    public class ObjectToFileCollectionConverter : ITypeConverter<object, FileCollectionForClient>
    {
        public FileCollectionForClient Convert(object source, FileCollectionForClient destination, ResolutionContext context)
        {
            dynamic dynamicSource = source;
            return DynamicConversionHelpers.ConvertDynamicToFileCollection(dynamicSource);
        }
    }

    public class ObjectToUrlFieldConverter : ITypeConverter<object, UrlFieldType>
    {
        public UrlFieldType? Convert(object source, UrlFieldType destination, ResolutionContext context)
        {
            UrlFieldType? result = null;
            if (source != null)
            {
                try
                {
                    dynamic dynamicSource = source;
                    string urlFieldTypeString = dynamicSource.ToString();
                    result = JsonConvert.DeserializeObject<UrlFieldType>(urlFieldTypeString);
                }
                catch
                {
                    result = null;
                }
            }

            return result;
        }
    }
}
