using Dino.CoreMvc.Admin.FieldTypePlugins;
using DinoGenericAdmin.Api.Models;
using Newtonsoft.Json;

namespace Dino.CoreMvc.Admin.Logic.Converters
{
    public static class DynamicConversionHelpers
    {
        private static Func<string, string> _pathConverter;

        public static void Initialize(Func<string, string> pathConverter)
        {
            _pathConverter = pathConverter ?? throw new ArgumentNullException(nameof(pathConverter));
        }

        public static FileCollectionForClient ConvertDynamicToFileCollection(dynamic fileCollection)
        {
            if (fileCollection == null)
            {
                return null;
            }

            try
            {
                // Create a new FileCollectionForClient
                var result = new FileCollectionForClient();
                
                // Convert the dynamic object to a dictionary of platform files
                var platformFiles = JsonConvert.DeserializeObject<Dictionary<string, List<dynamic>>>(fileCollection.ToString());
                if (platformFiles == null)
                {
                    return null;
                }

                // Convert each platform's files
                foreach (var platform in platformFiles)
                {
                    var filePaths = new List<string>();
                    foreach (var file in platform.Value)
                    {
                        // Convert the file path to a full URL using the provided path converter
                        filePaths.Add(_pathConverter(file.path.ToString()));
                    }
                    result[platform.Key.ToLower()] = filePaths;
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        public static List<T> ConvertDynamicList<T>(object items, Func<dynamic, T> converter)
        {
            if (items == null)
            {
                return null;
            }

            try
            {
                var result = new List<T>();
                var itemList = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(items));
                
                foreach (var item in itemList)
                {
                    result.Add(converter(item));
                }
                
                return result;
            }
            catch
            {
                return null;
            }
        }

        public static UrlFieldType ConvertDynamicToUrlFieldType(dynamic urlFieldType)
        {
            if (urlFieldType == null)
            {
                return null;
            }

            var urlFieldTypeString = urlFieldType.ToString();

            return JsonConvert.DeserializeObject<UrlFieldType>(urlFieldTypeString);
            
        }
    }
} 