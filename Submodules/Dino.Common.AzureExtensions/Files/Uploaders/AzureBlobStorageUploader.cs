using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Dino.Common.Helpers;
using Dino.Infra.Files.Uploaders;

namespace Dino.Common.AzureExtensions.Files.Uploaders
{
    public class AzureBlobStorageUploader : IFileUploader
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly string _baseLocation;

        public AzureBlobStorageUploader(string connectionString, string containerName, string baseLocation)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerName = containerName;
            _baseLocation = baseLocation;
        }

        public string GetFullPath(string path)
        {
            return Path.Combine(_baseLocation, path);
        }

        public Stream GetFileStream(string path)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(path);

            return blobClient.OpenRead(false);
        }

        public async Task<Stream> GetFileStreamAsync(string path)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(path);

            return await blobClient.OpenReadAsync(false);
        }

        public string UploadFile(FileUploadTask fileUploadTask)
        {
            var paths = UploadFiles(new List<FileUploadTask> { fileUploadTask });

            return paths.Count > 0 ? paths[0] : null;
        }

        public async Task<string> UploadFileAsync(FileUploadTask fileUploadTask)
        {
            var paths = await UploadFilesAsync(new List<FileUploadTask> { fileUploadTask });

            return paths.Count > 0 ? paths[0] : null;
        }

        public IList<string> UploadFiles(IEnumerable<FileUploadTask> fileUploadTasks)
        {
            var paths = new List<string>();

            foreach (var currTask in fileUploadTasks)
            {
                var currPath = Path.Combine(currTask.IsCustomPath ? string.Empty : _baseLocation, currTask.NewFilePath);

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(currPath);
                
                var blobHttpHeader = new BlobHttpHeaders();
                blobHttpHeader.ContentType = currTask.ContentType.IsNotNullOrEmpty() ? currTask.ContentType : "application/octet-stream";

                currTask.Stream.Seek(0, SeekOrigin.Begin);

                blobClient.Upload(currTask.Stream, blobHttpHeader);

                paths.Add(currTask.NewFilePath);
            }

            return paths;
        }

        public async Task<IList<string>> UploadFilesAsync(IEnumerable<FileUploadTask> fileUploadTasks)
        {
            var paths = new List<string>();

            foreach (var currTask in fileUploadTasks)
            {
                var currPath = Path.Combine(currTask.IsCustomPath ? string.Empty : _baseLocation, currTask.NewFilePath);

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(currPath);

                var blobHttpHeader = new BlobHttpHeaders();
                blobHttpHeader.ContentType = currTask.ContentType.IsNotNullOrEmpty() ? currTask.ContentType : "application/octet-stream";

                currTask.Stream.Seek(0, SeekOrigin.Begin);

                await blobClient.UploadAsync(currTask.Stream, blobHttpHeader);

                paths.Add(currTask.NewFilePath);
            }

            return paths;
        }

        public string SaveFile(byte[] file, string filePath, string contentType = "application/octet-stream")
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(filePath);

            var blobHttpHeader = new BlobHttpHeaders();
            blobHttpHeader.ContentType = contentType.IsNotNullOrEmpty() ? contentType : "application/octet-stream";

            using (var stream = new MemoryStream(file))
            {
                blobClient.Upload(stream, blobHttpHeader);
            }

            return filePath;
        }

        public async Task<string> SaveFileAsync(byte[] file, string filePath, string contentType = "application/octet-stream")
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(filePath);

            var blobHttpHeader = new BlobHttpHeaders();
            blobHttpHeader.ContentType = contentType.IsNotNullOrEmpty() ? contentType : "application/octet-stream";

            using (var stream = new MemoryStream(file))
            {
                await blobClient.UploadAsync(stream, blobHttpHeader);
            }

            return filePath;
        }

        public void DeleteFile(string filePath)
        {
            DeleteFiles(new List<string> { filePath });
        }

        public async Task DeleteFileAsync(string filePath)
        {
            await DeleteFilesAsync(new List<string> { filePath });
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            foreach (var currFilePath in filePaths)
            {
                containerClient.DeleteBlobIfExists(currFilePath);
            }
        }

        public async Task DeleteFilesAsync(IEnumerable<string> filePaths)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            foreach (var currFilePath in filePaths)
            {
                await containerClient.DeleteBlobIfExistsAsync(currFilePath);
            }
        }

        public async Task<byte[]> ReadFileAsByteArray(string filePath)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(filePath);

            var response = await blobClient.DownloadAsync();

            using (var memoryStream = new MemoryStream())
            {
                await response.Value.Content.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }



        public async Task RenameFiles(string oldFilePath, string newFilePath)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var oldFile = containerClient.GetBlobClient(oldFilePath);
            var newFile = containerClient.GetBlobClient(newFilePath);

            var upload = await oldFile.StartCopyFromUriAsync(newFile.Uri);

            long copiedContentLength = 0;
            while (!upload.HasCompleted)
            {
                copiedContentLength = await upload.WaitForCompletionAsync();
                await Task.Delay(100);

            }
            
            await oldFile.DeleteAsync();

        }
    }
}
