using Dino.Common.Files;
using Dino.Core.AdminBL;
using Dino.Core.AdminBL.Cache;
using Dino.Mvc.Common.Helpers;
using ForumSimpleAdmin.Api.Models;
using ForumSimpleAdmin.BL.Cache;
using ForumSimpleAdmin.BL.Contracts;
using ForumSimpleAdmin.BL.Data;
using Microsoft.Extensions.Options;

namespace ForumSimpleAdmin.Api.Controllers.Base
{
    public abstract class MainAppBaseController<T> : DinoController where T : MainAppBaseController<T>
    {
        private ILogger<T>? _logger;
        private BLFactory<MainDbContext, BlConfig, DinoCacheManager>? _blFactory;
        private IOptions<ApiConfig>? _apiConfig;
        private IOptions<BlConfig>? _blConfig;
        private DinoCacheManager? _dinoCacheManager;

        protected ILogger<T> Logger => _logger ??= HttpContext?.RequestServices.GetService<ILogger<T>>()!;
        protected BLFactory<MainDbContext, BlConfig, DinoCacheManager> BLFactory =>
            _blFactory ??= HttpContext?.RequestServices.GetService<BLFactory<MainDbContext, BlConfig, DinoCacheManager>>()!;
        protected IOptions<ApiConfig> ApiConfig => _apiConfig ??= HttpContext?.RequestServices.GetService<IOptions<ApiConfig>>()!;
        protected IOptions<BlConfig> BlConfig => _blConfig ??= HttpContext?.RequestServices.GetService<IOptions<BlConfig>>()!;
        protected DinoCacheManager DinoCacheManager => _dinoCacheManager ??= HttpContext?.RequestServices.GetService<DinoCacheManager>()!;

        protected TBL GetBL<TBL>(bool forceNewContext = false) where TBL : BaseBL<MainDbContext, BlConfig, DinoCacheManager>
        {
            TBL bl = BLFactory.GetBL<TBL>(forceNewContext);
            return bl;
        }

        protected FileUploadRequest? GetFileUploadRequest(string fileName)
        {
            FileUploadRequest? uploadFile = null;
            if (Request.Form.Files[fileName] != null)
            {
                uploadFile = new FileUploadRequest
                {
                    FileName = Request.Form.Files[fileName].FileName,
                    ContentType = Request.Form.Files[fileName].ContentType,
                    ContentLength = Request.Form.Files[fileName].Length,
                    FileStream = Request.Form.Files[fileName].OpenReadStream(),
                    DeleteOld = true
                };
            }

            return uploadFile;
        }
    }
}
