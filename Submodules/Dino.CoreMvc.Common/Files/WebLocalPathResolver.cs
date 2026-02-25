using System.IO;
using Dino.Common.Files;
using Dino.Common.Helpers;
using Microsoft.AspNetCore.Hosting;

namespace Dino.CoreMvc.Common.Files
{
    public class WebLocalPathResolver : ILocalPathResolver
    {
        private readonly IWebHostEnvironment _webEnv;

        public WebLocalPathResolver(IWebHostEnvironment webHostEnvironment)
        {
            _webEnv = webHostEnvironment;
        }

        public string Resolve(string path)
        {
            var rootPath = _webEnv.WebRootPath.IsNotNullOrEmpty() ? _webEnv.WebRootPath : _webEnv.ContentRootPath;
            return UrlHelpers.Combine(rootPath, path);
        }
    }
}
