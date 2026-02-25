using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Dino.Infra.Files.Uploaders
{
	public class FileUploadTask
	{
		public FileUploadTask(Stream stream, string newFilePath, bool isCustomPath = false, string contentType = "application/octet-stream")
		{
			Stream = stream;
			NewFilePath = newFilePath;
		    IsCustomPath = isCustomPath;
			ContentType = contentType;
		}

		public Stream Stream { get; set; }
		public string NewFilePath { get; set; }
        public bool IsCustomPath { get; set; }
		public string ContentType { get; set; }
	}

	public interface IFileUploader
	{
        /// <summary>
        /// Get the full path for the passed file path
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>Full file path</returns>
        string GetFullPath(string path);

        /// <summary>
        /// Get full path FileStream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file stream to the full file path.</returns>
        Stream GetFileStream(string path);

        /// <summary>
        /// Get full path FileStream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file stream to the full file path.</returns>
        Task<Stream> GetFileStreamAsync(string path);

        /// <summary>
        /// Uploads a file.
        /// </summary>
        /// <param name="fileUploadTask">The file upload task.</param>
        /// <returns>The uploaded file path.</returns>
        string UploadFile(FileUploadTask fileUploadTask);

        /// <summary>
        /// Uploads a file.
        /// </summary>
        /// <param name="fileUploadTask">The file upload task.</param>
        /// <returns>The uploaded file path.</returns>
        Task<string> UploadFileAsync(FileUploadTask fileUploadTask);

        /// <summary>
        /// Uploads a file.
        /// </summary>
        /// <param name="fileUploadTasks">The file upload tasks.</param>
        /// <returns>The uploaded file paths.</returns>
        IList<string> UploadFiles(IEnumerable<FileUploadTask> fileUploadTasks);

        /// <summary>
        /// Uploads a file.
        /// </summary>
        /// <param name="fileUploadTasks">The file upload tasks.</param>
        /// <returns>The uploaded file paths.</returns>
        Task<IList<string>> UploadFilesAsync(IEnumerable<FileUploadTask> fileUploadTasks);

        /// <summary>
        /// Saves a file from byte array.
        /// </summary>
        /// <param name="file">The file, as byte-array.</param>
        /// <param name="filePath">The file's path.</param>
        /// <param name="contentType">The file content type.</param>
        /// <returns>The uploaded file path.</returns>
        string SaveFile(byte[] file, string filePath, string contentType = "application/octet-stream");

        /// <summary>
        /// Saves a file from byte array.
        /// </summary>
        /// <param name="file">The file, as byte-array.</param>
        /// <param name="filePath">The file's path.</param>
        /// <param name="contentType">The file content type.</param>
        /// <returns>The uploaded file path.</returns>
        Task<string> SaveFileAsync(byte[] file, string filePath, string contentType = "application/octet-stream");

        /// <summary>
        /// Deletes a file. 
        /// </summary>
        /// <param name="filePath">The file path.</param>
        void DeleteFile(string filePath);

        /// <summary>
        /// Deletes a file. 
        /// </summary>
        /// <param name="filePath">The file path.</param>
        Task DeleteFileAsync(string filePath);

        /// <summary>
        /// Deletes a list of files. 
        /// </summary>
        /// <param name="filePaths">All file paths to delete.</param>
        void DeleteFiles(IEnumerable<string> filePaths);

        /// <summary>
        /// Deletes a list of files. 
        /// </summary>
        /// <param name="filePaths">All file paths to delete.</param>
        Task DeleteFilesAsync(IEnumerable<string> filePaths);

        /// <summary>
        /// Reads a file as a byte array.
        /// </summary>
        /// <param name="filePath">The file's path.</param>
        /// <returns>The byte array of the file, or NULL if nothing found.</returns>
        Task<byte[]> ReadFileAsByteArray(string filePath);
    }
}
