using System.Drawing.Drawing2D;
using Dino.Common.Helpers;
using Dino.Core.AdminBL;
using Dino.Core.AdminBL.Contracts;
using Dino.CoreMvc.Admin.Contracts;
using Z.EntityFramework.Extensions.Internal;

namespace Dino.CoreMvc.Admin.Logic.Helpers
{
    public static class PathHelpers
    {
        public static string GetUploadsFullPath(BaseApiConfig apiConfig, BaseBlConfig blConfig, string path, bool skipCdn = false)
        {
            if ((path == null) || path.StartsWith("http"))
            {
                // If the full path starts with the API path, and there's CDN, replace.
                if (!skipCdn && apiConfig.BaseCdnUrl.IsNotNullOrEmpty() &&
                    path.StartsWith(apiConfig.ApiBaseUrl))
                {
                    path = path.ReplaceFirst(apiConfig.ApiBaseUrl, apiConfig.BaseCdnUrl);
                }

                return path;
            }

            if (!path.StartsWith(apiConfig.UploadsFolder) && !blConfig.StorageConfig.UseAzureBlob)
            {
                path = UrlHelpers.Combine(apiConfig.UploadsFolder, path);
            }

            if (!skipCdn && apiConfig.BaseCdnUrl.IsNotNullOrEmpty() &&
                !blConfig.StorageConfig.UseAzureBlob)
            {
                path = Path.Combine(apiConfig.BaseCdnUrl, path);
            }
            else
            {
                path = GetContentFullPath(apiConfig, blConfig, path);

            }

            return path;
        }

        public static string GetContentFullPath(BaseApiConfig apiConfig, BaseBlConfig blConfig, string path)
        {
            if ((path == null) || path.StartsWith("http"))
            {
                return path;
            }

            var baseUrl = blConfig.StorageConfig.UseAzureBlob
                ? Path.Combine(blConfig.StorageConfig.AzureBlobBaseUrl, blConfig.StorageConfig.AzureBlobContainerName)
                : apiConfig.ApiBaseUrl;

            return UrlHelpers.Combine(baseUrl, path);
        }
    }
}
