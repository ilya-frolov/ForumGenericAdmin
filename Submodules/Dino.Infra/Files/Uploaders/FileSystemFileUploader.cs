using Dino.Common.Helpers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace Dino.Infra.Files.Uploaders
{
    public class FileSystemFileUploader : IFileUploader
    {
        public static string BaseLocation { get; private set; }

        public static void Init(string baseLocation)
        {
            BaseLocation = baseLocation;

            Directory.CreateDirectory(baseLocation);
        }

        public string GetFullPath(string path)
        {
            return Path.Combine(BaseLocation, path);
        }

        public Stream GetFileStream(string path)
        {
            var fullPath = GetFullPath(path);

            return File.OpenRead(fullPath);
        }

        public async Task<Stream> GetFileStreamAsync(string path)
        {
            return GetFileStream(path);
        }

        public string UploadFile(FileUploadTask fileUploadTask)
        {
            var paths = UploadFiles(fileUploadTask.ToArray());

            return (paths.Count > 0) ? paths[0] : null;
        }

        public async Task<string> UploadFileAsync(FileUploadTask fileUploadTask)
        {
            var paths = await UploadFilesAsync(fileUploadTask.ToArray());

            return (paths.Count > 0) ? paths[0] : null;
        }

        public IList<string> UploadFiles(IEnumerable<FileUploadTask> fileUploadTasks)
        {
            var paths = new List<string>();

            foreach (var currTask in fileUploadTasks)
            {
                var currPath = currTask.IsCustomPath ? currTask.NewFilePath :  GetFullPath(currTask.NewFilePath);

                // Make sure the path exists
                Directory.CreateDirectory(Path.GetDirectoryName(currPath));

                using (var fileStream = File.Create(currPath))
                {
                    currTask.Stream.Seek(0, SeekOrigin.Begin);
                    currTask.Stream.CopyTo(fileStream);
                }

                paths.Add(currTask.NewFilePath);
            }

            return paths;
        }

        public async Task<IList<string>> UploadFilesAsync(IEnumerable<FileUploadTask> fileUploadTasks)
        {
            var paths = new List<string>();

            foreach (var currTask in fileUploadTasks)
            {
                var currPath = currTask.IsCustomPath ? currTask.NewFilePath : GetFullPath(currTask.NewFilePath);

                // Make sure the path exists
                Directory.CreateDirectory(Path.GetDirectoryName(currPath));

                using (var fileStream = File.Create(currPath))
                {
                    currTask.Stream.Seek(0, SeekOrigin.Begin);

#if NETSTANDARD2_1_OR_GREATER
                    await currTask.Stream.CopyToAsync(fileStream);
#else
                    currTask.Stream.CopyTo(fileStream);
#endif
                }

                paths.Add(currTask.NewFilePath);
            }

            return paths;
        }

        public string SaveFile(byte[] file, string filePath, string contentType = "application/octet-stream")
        {
            var currPath = GetFullPath(filePath);

            // Make sure the path exists
            Directory.CreateDirectory(Path.GetDirectoryName(currPath));

            File.WriteAllBytes(currPath, file);

            return currPath;
        }

        public async Task<string> SaveFileAsync(byte[] file, string filePath, string contentType = "application/octet-stream")
        {
            var currPath = GetFullPath(filePath);

            // Make sure the path exists
            Directory.CreateDirectory(Path.GetDirectoryName(currPath));

#if NETSTANDARD2_1_OR_GREATER
            await File.WriteAllBytesAsync(currPath, file);
#else
            File.WriteAllBytes(currPath, file);
#endif

            return currPath;
        }

        public void DeleteFile(string filePath)
        {
            DeleteFiles(filePath.ToArray());
        }

        public async Task DeleteFileAsync(string filePath)
        {
            await DeleteFilesAsync(filePath.ToArray());
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            foreach (var currFilePath in filePaths)
            {
                var currPath = GetFullPath(currFilePath);

                File.Delete(currPath);
            }
        }

        public async Task DeleteFilesAsync(IEnumerable<string> filePaths)
        {
            DeleteFiles(filePaths);
        }

        public async Task<byte[]> ReadFileAsByteArray(string filePath)
        {
#if NETSTANDARD2_1_OR_GREATER
            return await File.ReadAllBytesAsync(filePath);
#else
            return File.ReadAllBytes(filePath);
#endif


        }
    }
}
