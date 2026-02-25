using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dino.Common.AzureExtensions.Files.Uploaders;
using Dino.Core.AdminBL.Contracts;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.Contracts;
using Dino.CoreMvc.Admin.Logic.Helpers;
using Dino.Infra.Files.Uploaders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Dino.CoreMvc.Admin.FieldTypePlugins.Plugins.Base
{
    /// <summary>
    /// Base plugin for file-like fields (file/picture).
    /// </summary>
    public abstract class FileFieldBasePlugin<TAttribute> : BaseFieldTypePlugin<TAttribute, object>
        where TAttribute : AdminFieldFileAttribute
    {
        private IOptions<BaseBlConfig> _blConfig;
        private IOptions<BaseApiConfig> _apiConfig;
        private readonly IServiceProvider _serviceProvider;

        protected FileFieldBasePlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _blConfig ??= _serviceProvider.GetService<IOptions<BaseBlConfig>>();
            _apiConfig ??= _serviceProvider.GetService<IOptions<BaseApiConfig>>();
        }

        /// <summary>
        /// Validates a file field value.
        /// </summary>
        public override (bool IsValid, List<string> ErrorMessages) Validate(object value, PropertyInfo property)
        {
            // First validate using the base validation (required check, etc.)
            var baseResult = base.Validate(value, property);

            // If already invalid, no need to continue
            if (!baseResult.IsValid)
                return baseResult;

            var errorMessages = baseResult.ErrorMessages;
            var fileAttr = property.GetCustomAttribute<TAttribute>();

            // If file attribute is null or value is null, skip additional validation
            if (fileAttr == null || value == null)
                return (errorMessages.Count == 0, errorMessages);

            try
            {
                // Parse the file info map from either string or direct object
                FileContainerCollection collection = null;

                if (value is string json && !string.IsNullOrEmpty(json))
                {
                    collection = JsonConvert.DeserializeObject<FileContainerCollection>(json);
                }
                else if (value is FileContainerCollection map)
                {
                    collection = map;
                }

                if (collection == null)
                    return (errorMessages.Count == 0, errorMessages);

                // Validate all files against allowed extensions if specified
                if (fileAttr.AllowedExtensions != null && fileAttr.AllowedExtensions.Length > 0)
                {
                    foreach (var platformEntry in collection.PlatformFiles)
                    {
                        foreach (var fileInfo in platformEntry.Value)
                        {
                            // Get filename from path or use Name property
                            string fileName = System.IO.Path.GetFileName(fileInfo.Path);

                            // Validate file extension
                            bool validExtension = false;
                            foreach (var ext in fileAttr.AllowedExtensions)
                            {
                                if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                                {
                                    validExtension = true;
                                    break;
                                }
                            }

                            if (!validExtension)
                            {
                                errorMessages.Add($"File '{fileName}' has an invalid extension. Allowed extensions: {string.Join(", ", fileAttr.AllowedExtensions)}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessages.Add($"Invalid file data format: {ex.Message}");
            }

            return (errorMessages.Count == 0, errorMessages);
        }

        /// <summary>
        /// Prepares a file collection value for database storage.
        /// </summary>
        protected override object PrepareTypedValueForDb(object value, PropertyInfo property)
        {
            if (value == null)
                return null;

            try
            {
                // Parse the file info map from either string or direct object
                FileContainerCollection collection = new FileContainerCollection();

                if (value is string json && !string.IsNullOrEmpty(json))
                {
                    collection = JsonConvert.DeserializeObject<FileContainerCollection>(json);
                }
                else if (value is FileContainerCollection map)
                {
                    collection = map;
                }

                if (collection != null)
                {
                    // Get the file attribute to check supported platforms
                    var fileAttr = property.GetCustomAttribute<TAttribute>();
                    var supportedPlatforms = fileAttr?.Platforms;

                    // Process file uploads and deletions
                    ProcessFileOperations(collection);

                    // Convert back to a dictionary for JSON storage
                    var resultPlatformFiles = new Dictionary<string, List<MiniFileContainerForDB>>();

                    // For each platform that's supported, add its files
                    foreach (Platforms platform in Enum.GetValues(typeof(Platforms)))
                    {
                        // Check if this platform is supported
                        if (supportedPlatforms == null || (supportedPlatforms & platform) != 0)
                        {
                            var files = collection.GetFiles(platform);

                            // Skip deleted files
                            var nonDeletedFiles = files.Where(f => !f.IsMarkedForDeletion).ToList();

                            if (nonDeletedFiles.Count > 0)
                            {
                                // Convert to FileContainer objects
                                var fileInfoList = nonDeletedFiles.Select(f => new MiniFileContainerForDB
                                {
                                    Path = f.Path,
                                    Name = f.Name,
                                    Size = f.Size,
                                }).ToList();

                                resultPlatformFiles[platform.ToString()] = fileInfoList;
                            }
                        }
                    }

                    // Serialize to JSON for database storage
                    return JsonConvert.SerializeObject(resultPlatformFiles);
                }

                // If it's already a string and we couldn't parse it as FileInfoMap,
                // return it as is (for backwards compatibility)
                if (value is string)
                    return value;

                // Otherwise serialize the object
                return JsonConvert.SerializeObject(value);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in PrepareTypedValueForDb: {ex.Message}");

                // If parsing fails, convert to string if needed
                if (value is string)
                    return value;

                return JsonConvert.SerializeObject(value);
            }
        }

        protected IFileUploader GetFileUploader(string containerName = null)
        {
            return _blConfig.Value.StorageConfig.UseAzureBlob
               ? new AzureBlobStorageUploader(_blConfig.Value.StorageConfig.AzureBlobConnectionString, containerName ?? _blConfig.Value.StorageConfig.AzureBlobContainerName, "")
               : new FileSystemFileUploader();
        }

        /// <summary>
        /// Processes file upload and deletion operations.
        /// </summary>
        protected void ProcessFileOperations(FileContainerCollection collection)
        {
            if (collection == null)
                return;

            // Try to get the IFileUploader service to handle file operations
            var fileUploader = GetFileUploader();
            if (fileUploader == null)
                return;

            var uploadFolderName = _apiConfig.Value.UploadsFolder ?? "uploadsDefault";

            // Process deletions
            var filesToDelete = collection.AllFiles()
                .Where(f => f.IsMarkedForDeletion && !string.IsNullOrEmpty(f.Path))
                .Select(f => f.Path.Split($"/{uploadFolderName}/")?.Length == 2 ? f.Path.Split($"/{uploadFolderName}/")?[1] : f.Path)
                .ToList();

            if (filesToDelete.Count > 0)
            {
                try
                {
                    // Delete files from storage
                    fileUploader.DeleteFiles(filesToDelete);
                }
                catch (Exception)
                {
                    // Log error or handle failed deletions
                }
            }

            // Process uploads - find all files with pending uploads
            foreach (var file in collection.AllFiles())
            {
                if (file.IsMarkedForDeletion)
                    continue;

                // Process files with pending uploads
                if (file.PendingUpload != null)
                {
                    try
                    {
                        // Create upload task
                        var uploadTask = new FileUploadTask(
                            file.PendingUpload.Stream,
                            file.PendingUpload.FileName,
                            isCustomPath: false,
                            contentType: file.PendingUpload.ContentType
                        );

                        // Upload the file
                        string uploadedPath = fileUploader.UploadFile(uploadTask);

                        // Update the file path with the result
                        file.Path = uploadedPath;

                        // Clear the pending upload to avoid duplicate uploads
                        file.PendingUpload = null;
                    }
                    catch (Exception)
                    {
                        // Log error or handle failed uploads
                    }
                }
            }
        }

        /// <summary>
        /// Prepares a database value for model use.
        /// </summary>
        protected override object PrepareTypedValueForModel(object dbValue, PropertyInfo property)
        {
            // If no value, return null
            if (dbValue == null)
                return null;

            try
            {
                // If it's already a FileInfoMap, return it directly
                if (dbValue is FileContainerCollection map)
                    return map;

                // If it's a string, try to deserialize it
                if (dbValue is string json && !string.IsNullOrEmpty(json))
                {
                    // If it's not JSON, and just a single-path (like "uploads/123/file.jpg") - we need to convert it to a file collection.
                    if (!json.TrimStart().StartsWith("{") && !json.TrimStart().StartsWith("["))
                    {
                        return CreateCollectionFromSinglePath(json, property);
                    }

                    // Try to deserialize directly to FileInfoMap
                    FileContainerCollection containerCollection = new FileContainerCollection();
                    containerCollection.PlatformFiles = JsonConvert.DeserializeObject<Dictionary<Platforms, List<FileContainer>>>(json);
                    if (containerCollection.PlatformFiles != null)
                    {
                        // attach the base url to the file paths
                        foreach (var files in containerCollection.PlatformFiles.Values)
                        {
                            files.ForEach(file => file.Path = PathHelpers.GetUploadsFullPath(_apiConfig.Value, _blConfig.Value, file.Path));
                        }
                        return containerCollection;
                    }

                    // If all parsing fails, return the original JSON
                    return json;
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in PrepareTypedValueForModel: {ex.Message}");

                // If everything fails, return the original value
                return dbValue;
            }

            return dbValue;
        }

        /// <summary>
        /// Creates a file collection from a single path, for compatibility with DB values that are stored as a single path (in non-JSON format).
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="property">The property info.</param>
        /// <returns>The file collection.</returns>
        private FileContainerCollection CreateCollectionFromSinglePath(string path, PropertyInfo property)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var fileAttr = property.GetCustomAttribute<TAttribute>();
            var supportedPlatforms = fileAttr?.Platforms;

            var collection = new FileContainerCollection();
            var normalizedPath = PathHelpers.GetUploadsFullPath(_apiConfig.Value, _blConfig.Value, path);
            var file = new FileContainer
            {
                Path = normalizedPath,
                Name = System.IO.Path.GetFileName(path)
            };

            // Find the platform that the file is associated with, based on the supported platforms (just the first one that matches).
            Platforms? targetPlatform = null;
            foreach (Platforms platform in Enum.GetValues(typeof(Platforms)))
            {
                if (supportedPlatforms == null || (supportedPlatforms & platform) != 0)
                {
                    targetPlatform = platform;
                    break;
                }
            }

            // Add the file to the collection for the target platform.
            if (targetPlatform != null)
            {
                collection.AddFile(targetPlatform.Value, file);
            }

            return collection;
        }
    }
}
