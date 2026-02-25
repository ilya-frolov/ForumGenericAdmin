using AutoMapper;
using ForumSimpleAdmin.BL.Forum;

namespace ForumSimpleAdmin.BL.Cache
{
    public class BLAutoMapperProfile : Profile
    {
        private static Func<string, string>? _getFullUploadsPathFunc;

        public BLAutoMapperProfile(IServiceProvider serviceProvider, Func<string, string> getFullUploadsPathFunc)
        {
            _getFullUploadsPathFunc = getFullUploadsPathFunc;
            CreateMap<ForumPostPreviewDto, ForumPostPreviewDto>();
        }

        public static string GetFullUploadsPath(string path)
        {
            string result = path;
            if (_getFullUploadsPathFunc != null)
            {
                result = _getFullUploadsPathFunc(path);
            }

            return result;
        }
    }
}
