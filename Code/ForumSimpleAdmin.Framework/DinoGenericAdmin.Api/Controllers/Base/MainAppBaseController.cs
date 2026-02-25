using Dino.Common.Files;
using Dino.Core.AdminBL;
using Dino.Core.AdminBL.Cache;
using Dino.Mvc.Common.Helpers;
using DinoGenericAdmin.Api.Models;
using DinoGenericAdmin.BL;
using DinoGenericAdmin.BL.Cache;
using DinoGenericAdmin.BL.Contracts;
using DinoGenericAdmin.BL.Data;
using Microsoft.Extensions.Options;

namespace DinoGenericAdmin.Api.Controllers.Base
{
    public abstract class MainAppBaseController<T> : DinoController where T : MainAppBaseController<T>
    {
        private ILogger<T> _logger;
        private BLFactory<MainDbContext, BlConfig, DinoCacheManager> _blFactory;
        private IOptions<ApiConfig> _apiConfig;
        private IOptions<BlConfig> _blConfig;
        private DinoCacheManager _dinoCacheManager;
        //private IBackgroundJobClient _backgroundJobs;

        protected ILogger<T> Logger => _logger ??= HttpContext?.RequestServices.GetService<ILogger<T>>();
        protected BLFactory<MainDbContext, BlConfig, DinoCacheManager> BLFactory => _blFactory ??= HttpContext?.RequestServices.GetService<BLFactory<MainDbContext, BlConfig, DinoCacheManager>>();
        protected IOptions<ApiConfig> ApiConfig => _apiConfig ??= HttpContext?.RequestServices.GetService<IOptions<ApiConfig>>();
        protected IOptions<BlConfig> BlConfig => _blConfig ??= HttpContext?.RequestServices.GetService<IOptions<BlConfig>>();
        protected DinoCacheManager DinoCacheManager => _dinoCacheManager ??= HttpContext?.RequestServices.GetService<DinoCacheManager>();
        //protected IBackgroundJobClient BackgroundJobs => _backgroundJobs ??= HttpContext?.RequestServices.GetService<IBackgroundJobClient>();

        protected TBL GetBL<TBL>(bool forceNewContext = false) where TBL : BaseBL<MainDbContext, BlConfig, DinoCacheManager>
        {
            return BLFactory.GetBL<TBL>(forceNewContext);
        }

        #region FileUploads

        protected FileUploadRequest GetFileUploadRequest(string fileName)
        {
            FileUploadRequest uploadFile = null;
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


        //protected FileUploadRequest GetFileUploadRequest(FileContainer fileContainer, string fileName)
        //{
        //    FileUploadRequest uploadFile = null;
        //    if (fileContainer != null)
        //    {
        //        if (fileContainer.Upload && (Request.Form.Files[fileName] != null))
        //        {
        //            uploadFile = new FileUploadRequest
        //            {
        //                FileName = Request.Form.Files[fileName].FileName,
        //                ContentType = Request.Form.Files[fileName].ContentType,
        //                ContentLength = Request.Form.Files[fileName].Length,
        //                FileStream = Request.Form.Files[fileName].OpenReadStream(),
        //                DeleteOld = true
        //            };
        //        }
        //        else if (fileContainer.Delete)
        //        {
        //            uploadFile = new FileUploadRequest
        //            {
        //                DeleteOld = true
        //            };
        //        }
        //    }

        //    return uploadFile;
        //}

        //protected List<FileUploadRequest> GetFileUploadRequests(string filePrefix)
        //{
        //    var uploadFiles = new List<FileUploadRequest>();

        //    foreach (var currFile in Request.Form.Files)
        //    {
        //        if (currFile.Name.StartsWith(filePrefix))
        //        {
        //            uploadFiles.Add(new FileUploadRequest
        //            {
        //                FileName = currFile.FileName,
        //                ContentType = currFile.ContentType,
        //                ContentLength = currFile.Length,
        //                FileStream = currFile.OpenReadStream(),
        //                DeleteOld = true
        //            });
        //        }
        //    }

        //    return uploadFiles;
        //}

        //protected List<FileUploadRequest> GetFileUploadRequests(List<FileContainer> fileContainers, string filePrefix)
        //{
        //    var uploadFiles = new List<FileUploadRequest>();

        //    if (fileContainers.IsNotNullOrEmpty())
        //    {
        //        for (var i = 0; i < fileContainers.Count; i++)
        //        {
        //            var currFileContainer = fileContainers[i];

        //            FileUploadRequest uploadFile = null;
        //            var fileName = $"{filePrefix}-{i}";
        //            if (currFileContainer.Upload && (Request.Form.Files[fileName] != null))
        //            {
        //                uploadFile = new FileUploadRequest
        //                {
        //                    FileName = Request.Form.Files[fileName].FileName,
        //                    ContentType = Request.Form.Files[fileName].ContentType,
        //                    ContentLength = Request.Form.Files[fileName].Length,
        //                    FileStream = Request.Form.Files[fileName].OpenReadStream(),
        //                    DeleteOld = true
        //                };
        //            }
        //            else if (currFileContainer.Delete)
        //            {
        //                uploadFile = new FileUploadRequest
        //                {
        //                    FileName = currFileContainer.Path,
        //                    DeleteOld = true
        //                };
        //            }

        //            if (uploadFile != null)
        //            {
        //                uploadFiles.Add(uploadFile);
        //            }
        //        }
        //    }

        //    return uploadFiles;
        //}

        #endregion

        // TODO: Get this back.

        //protected int? GetLoggedInUserId()
        //{
        //    return LoginHelper.GetLoggedInUserId(HttpContext);
        //}

        //protected bool IsNormalUser()
        //{
        //    return HttpContext.User.IsInRole("User");
        //}

        //protected int? GetLoggedInAdminUserId()
        //{
        //    return LoginHelper.GetLoggedInAdminUserId(HttpContext);
        //}

        //protected bool IsAdminUser()
        //{
        //    return HttpContext.User.IsInRole("Admin");
        //}

        //protected string GetFullUploadsPath(string path)
        //{
        //    return PathHelpers.GetUploadsFullPath(ApiConfig.Value, BlConfig.Value, path);
        //}
    }
}
