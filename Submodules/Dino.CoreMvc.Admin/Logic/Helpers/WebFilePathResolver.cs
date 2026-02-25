using Dino.Common.Files;
using Dino.Core.AdminBL.Contracts;
using Dino.CoreMvc.Admin.Contracts;
using Microsoft.Extensions.Options;
using PathHelpers = Dino.CoreMvc.Admin.Logic.Helpers.PathHelpers;

namespace Dino.CoreMvc.Admin.Logic.Helpers
{
    public class WebFilePathResolver : IWebFilePathResolver
    {
        private readonly IOptions<BaseApiConfig> _apiConfig;
        private readonly IOptions<BaseBlConfig> _blConfig;

        public WebFilePathResolver(IOptions<BaseApiConfig> apiConfig, IOptions<BaseBlConfig> blConfig)
        {
            _apiConfig = apiConfig;
            _blConfig = blConfig;
        }

        public string Resolve(string path)
        {
            return PathHelpers.GetContentFullPath(_apiConfig.Value, _blConfig.Value, path);
        }

        public string ResolveUpload(string path)
        {
            return PathHelpers.GetUploadsFullPath(_apiConfig.Value, _blConfig.Value, path);
        }
    }
}
