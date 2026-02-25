using AutoMapper;
using Dino.Common.Helpers;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.FieldTypePlugins;
using DinoGenericAdmin.Api.Models;
using Newtonsoft.Json;

namespace Dino.CoreMvc.Admin.AutoMapper
{
    public abstract class AdminBaseMapperProfile : Profile
    {
        protected readonly IServiceProvider _serviceProvider;

        public AdminBaseMapperProfile(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // Shared Mappings
            //CreateMap<string, FileContainer>().
            //    ForMember(dest => dest.Path, opt => opt.MapFrom(src => GetUploadsPath(src)));

            CreateMap<DateTime, SerializableDateTime>()
                .ConvertUsing(src => new SerializableDateTime(src));

            CreateMap<SerializableDateTime, DateTime>()
                .ConvertUsing(model => model.ToDateTime());

            CreateMap<TimeSpan, SerializableTimeSpan>()
                .ConvertUsing(src => new SerializableTimeSpan(src));

            CreateMap<SerializableTimeSpan, TimeSpan>()
                .ConvertUsing(model => model.ToTimeSpan());

            CreateMap<string, FileContainerCollection>()
                .ConvertUsing(model => !string.IsNullOrEmpty(model)
                    ? new FileContainerCollection
                    {
                        PlatformFiles = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<Platforms, List<FileContainer>>>(model)
                    }
                    : null);

            CreateMap<FileContainerCollection, FileCollectionForClient>()
                .ConvertUsing(model => ConvertToFileCollectionForClient(model));

            CreateMap<string, FileCollectionForClient>()
                .ConvertUsing(model => ConvertJsonToFileCollectionForClient(model));

            CreateMap<string, UrlFieldType>()
                .ConvertUsing(model => JsonConvert.DeserializeObject<UrlFieldType>(model));

            CreateMap<UrlFieldType, string>()
                .ConvertUsing(model => JsonConvert.SerializeObject(model));

            // CreateMap<FileContainerCollection, Dictionary<string, List<string>>>()
            //     .ConvertUsing(model => 
            //         new Dictionary<string, List<string>> { 
            //               {Enum.GetName(typeof(Platforms), (Platforms)model.PlatformFiles.Keys.First()), model.PlatformFiles[Platforms.Web].Select(f => f.Path).ToList() }
            //           });

            AdminMappings();
            RegisterAllSelfMappings();

            ClientMappings();
        }

        protected abstract void AdminMappings();

        private void RegisterAllSelfMappings()
        {
            var assembly = GetType().Assembly;

            var type = typeof(IAutoMapperSelfConfigurator);
            var types = assembly.GetTypes()
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                .ToList();

            foreach (var currMapperTpye in types)
            {
                ((IAutoMapperSelfConfigurator)Activator.CreateInstance(currMapperTpye)).AutoMappingConfiguration(this);
            }
        }

        protected abstract void ClientMappings();
        protected abstract string GetUploadsPath(string path);

        private FileCollectionForClient ConvertToFileCollectionForClient(FileContainerCollection model)
        {
            if (model == null || model.PlatformFiles == null)
            {
                return null;
            }

            var clientCollection = new FileCollectionForClient();
            var transformedData = model.PlatformFiles.ToDictionary(
                kvp => Enum.GetName(typeof(Platforms), kvp.Key),
                kvp => kvp.Value.Select(fc => GetUploadsPath(fc.Path)).ToList()
            );

            foreach (var entry in transformedData)
            {
                clientCollection.Add(entry.Key, entry.Value);
            }
            return clientCollection;
        }

        private FileCollectionForClient ConvertJsonToFileCollectionForClient(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                var deserializedData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<Platforms, List<FileContainer>>>(json);

                if (deserializedData == null)
                {
                    return null;
                }

                var clientCollection = new FileCollectionForClient();
                var transformedData = deserializedData.ToDictionary(
                    kvp => Enum.GetName(typeof(Platforms), kvp.Key),
                    kvp => kvp.Value.Select(fc => GetUploadsPath(fc.Path)).ToList()
                );

                foreach (var entry in transformedData)
                {
                    clientCollection.Add(entry.Key, entry.Value);
                }
                return clientCollection;
            }
            catch (JsonException /* ex */) // Catch specific JsonException, variable ex is unused for now
            {
                return null; // Or return new FileCollectionForClient();
            }
        }
    }
}
