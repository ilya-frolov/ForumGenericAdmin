using System;
using System.IO;
using System.Net;
using Dino.Infra.Files.Uploaders;

namespace Dino.Infra.Files.Handlers
{
	public abstract class BaseFileHandler
	{
		protected IFileUploader FileUploader { get; private set; }
		protected Stream FileStream { get; set; }
		
		private string _allowedExtensions = String.Empty;
		private int _maxFileSizeInKbs = 1024;

		public FilePathGenerator NewFilePathGenerator { get; set; }
		public string FileName { get; set; }

		public string AllowedExtensions
		{
			get { return _allowedExtensions; }
			set { _allowedExtensions = value; }
		}

		public int MaxFileSizeInKbs
		{
			get { return _maxFileSizeInKbs; }
			set { _maxFileSizeInKbs = value; }
		}

		protected BaseFileHandler(IFileUploader fileUploader, string filePath, FilePathGenerator newPathGenerator)
		{
			FileUploader = fileUploader;
			NewFilePathGenerator = newPathGenerator;
			FileName = Path.GetFileName(filePath);

			// Checks if this is a web uri to handle it accordingly
			if (Uri.IsWellFormedUriString(filePath, UriKind.RelativeOrAbsolute))
			{
				LoadFileStreamFromUri(new Uri(filePath));
			}
			else
			{
				LoadFileStreamFromPath(filePath);
			}
		}

		protected BaseFileHandler(IFileUploader fileUploader, Stream fileStream, string fileName, FilePathGenerator newPathGenerator)
		{
			FileUploader = fileUploader;
			NewFilePathGenerator = newPathGenerator;
			FileName = fileName;
			
			FileStream = fileStream;
			FileStream.Position = 0;
		}

		protected void LoadFileStreamFromPath(string path)
		{
			FileStream = File.OpenRead(path);
		}

		protected void LoadFileStreamFromUri(Uri fileUri)
		{
			var client = new WebClient();

			FileStream = client.OpenRead(fileUri);
		}

		/// <summary>
		/// Uploads the loaded file.
		/// </summary>
		/// <returns>The relative path the file was uploaded to.</returns>
		public virtual string UploadFile()
		{
			// Resets the position of the stream so we may be able to read the file
			FileStream.Position = 0;

			// Uploads the file using the uploader and returns the relative path
			return FileUploader.UploadFile(new FileUploadTask(FileStream, NewFilePathGenerator.GeneratePath(FileName)));
		}
	}
}
