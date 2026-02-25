using System.Linq.Expressions;
using AutoMapper;
using Dino.Common.AzureExtensions.Files.Uploaders;
using Dino.Common.Files;
using Dino.Common.Helpers;
using Dino.Infra.Files.Uploaders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Dino.Core.AdminBL.Contracts;
using Dino.Core.AdminBL.Cache;
using Dino.Core.AdminBL.Data;

namespace Dino.Core.AdminBL
{
    public abstract class BaseBL<TDbContext, TBlConfig, TCacheManager>
        where TDbContext : BaseDbContext<TDbContext>
        where TBlConfig : BaseBlConfig
        where TCacheManager : BaseDinoCacheManager<TDbContext, TBlConfig, TCacheManager>
    {
        protected ILogger Logger;
        protected readonly TDbContext Db;
        protected readonly BLFactory<TDbContext, TBlConfig, TCacheManager> BlFactory;
        protected readonly TBlConfig BlConfig;
        protected readonly TCacheManager Cache;
        protected readonly IMapper Mapper;
        private readonly List<Func<Task>> _afterSuccessfulSaveActions = new List<Func<Task>>();

        protected BaseBL(BLFactory<TDbContext, TBlConfig, TCacheManager> factory, TDbContext context, IMapper mapper)
        {
            BlFactory = factory;
            BlConfig = factory.GetConfig();
            Cache = factory.GetCacheManager();
            Db = context;
            Logger = factory.GetLogger(GetType());
            Mapper = mapper;
        }


        #region RegisterAfterSuccessfulSaveAction

        protected void RegisterAfterSuccessfulSaveAction(Func<Task> action)
        {
            _afterSuccessfulSaveActions.Add(action);
        }

        #endregion

        #region SaveChanges

        public int SaveChanges()
        {
            var result = 0;

            try
            {
                result = Db.SaveChanges();

                RunNextAction();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public async Task<int> SaveChangesAsync()
        {
            var result = 0;

            try
            {
                result = await Db.SaveChangesAsync();

                RunNextAction();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        #endregion

        #region RunNextAction

        private void RunNextAction()
        {
            // Calls the first action asynchronously (The other actions will be called afterwards)
            var actionToRun = _afterSuccessfulSaveActions.FirstOrDefault();
            if (actionToRun != null)
            {
                _afterSuccessfulSaveActions.RemoveAt(0);

                try
                {
                    _ = Task.Run(async () =>
                    {
                        await actionToRun();
                        RunNextAction();
                    });
                }
                catch (Exception)
                {
                    // TODO: Log	
                }
            }
        }

        #endregion

        #region ApplyOrderAndSkip

        //protected IQueryable<T> ApplyOrderAndSkip<T>(IQueryable<T> q, ListFilters filters)
        //{
        //    q = OrderByColumnName(q, filters.SortColumn, filters.SortDescending);

        //    q = q.Skip(filters.PageIndex * filters.PageSize)
        //         .Take(filters.PageSize);

        //    return q;
        //}

        #endregion

        #region Files

        #region GetFileUploader

        protected IFileUploader GetFileUploader()
        {
            return BlConfig.StorageConfig.UseAzureBlob ?
                new AzureBlobStorageUploader(BlConfig.StorageConfig.AzureBlobConnectionString, BlConfig.StorageConfig.AzureBlobContainerName, BlConfig.StorageConfig.AzureBlobBaseUrl) :
                new FileSystemFileUploader();
        }

        #endregion

        #region UploadFile

        protected async Task<string> UploadFile(FileUploadRequest file, string existingPath, string uploadFolder, bool isFullPath = false)
        {
            var returnPath = existingPath;

            if (file != null)
            {
                var uploader = GetFileUploader();

                if (file.DeleteOld && existingPath.IsNotNullOrEmpty())
                {
                    try
                    {
                        await uploader.DeleteFileAsync(existingPath);
                    }
                    catch (Exception e)
                    {
                        // We allow the delete to fail
                    }

                    returnPath = null;
                }

                if (file.FileStream != null)
                {
                    var filePath = isFullPath ? uploadFolder : $"{uploadFolder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var imageUploadTask = new FileUploadTask(file.FileStream, filePath);
                    returnPath = await uploader.UploadFileAsync(imageUploadTask);
                }
            }

            return returnPath;
        }

        #endregion

        #region UploadFiles

        protected async Task<List<string>> UploadFiles(List<FileUploadRequest> files, List<string> existingPaths, string uploadFolder)
        {
            var updatedPaths = new List<string>(existingPaths);

            if (files.IsNotNullOrEmpty())
            {
                var uploader = GetFileUploader();

                foreach (var currFile in files)
                {
                    if (currFile.DeleteOld && currFile.FileName.IsNotNullOrEmpty() && updatedPaths.Contains(currFile.FileName))
                    {
                        try
                        {
                            await uploader.DeleteFileAsync(currFile.FileName);
                            updatedPaths.Remove(currFile.FileName);
                        }
                        catch (Exception e)
                        {
                            // We allow the delete to fail
                        }
                    }

                    if (currFile.FileStream != null)
                    {
                        var imageUploadTask = new FileUploadTask(currFile.FileStream, $"{uploadFolder}/{Guid.NewGuid()}{Path.GetExtension(currFile.FileName)}");
                        updatedPaths.Add(await uploader.UploadFileAsync(imageUploadTask));
                    }
                }
            }

            return updatedPaths;
        }

        #endregion

        #region UploadFilesSerialized

        protected async Task<string> UploadFilesSerialized(List<FileUploadRequest> files, string existingPathsSerialized, string uploadFolder)
        {
            var existingFiles = existingPathsSerialized.IsNotNullOrEmpty() ? JsonConvert.DeserializeObject<List<string>>(existingPathsSerialized) : new List<string>();

            var newPaths = await UploadFiles(files, existingFiles, uploadFolder);

            return JsonConvert.SerializeObject(newPaths);
        }

        #endregion

        #endregion

        #region Commons

        public virtual T GetById<T, TId>(TId id) where T : class
        {
            return Db.Set<T>().Find(id);
        }

        public virtual IQueryable<T> GetAll<T>(int? limit = null, int? page = null) where T : class
        {
            IQueryable<T> items = Db.Set<T>();

            return GetAll(items, limit, page);
        }

        public virtual IQueryable<T> GetAll<T>(int? limit, int? page, out int count) where T : class
        {
            IQueryable<T> items = Db.Set<T>();

            return GetAll(items, limit, page, out count);
        }

        public virtual IQueryable<T> GetAll<T>(IQueryable<T> items, int? limit = null, int? page = null) where T : class
        {
            // Limit and select relevant page.
            if (page.HasValue && limit.HasValue)
            {
                items = items.Skip(page.Value * limit.Value);
            }

            if (limit.HasValue)
            {
                items = items.Take(limit.Value);
            }

            return items;
        }

        public virtual IQueryable<T> GetAll<T>(IQueryable<T> items, int? limit, int? page, out int count) where T : class
        {
            count = items.Count();

            items = GetAll(items, limit, page);

            return items;
        }




        public virtual bool DeleteById<T, TId>(TId id) where T : class
        {
            bool deleted = false;

            var item = Db.Set<T>().Find(new { id });
            if (item != null)
            {
                Db.Set<T>().Remove(item);
                deleted = Db.SaveChanges() > 0;
            }

            return deleted;
        }

        #endregion
    }
}
