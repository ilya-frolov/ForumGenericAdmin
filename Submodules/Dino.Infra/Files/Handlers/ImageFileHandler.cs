using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Dino.Common.Files;
using Dino.Infra.Files.Uploaders;

namespace Dino.Infra.Files.Handlers
{
	public class ImageFileHandler : BaseFileHandler
	{
		private Bitmap _bitmap;

		public IList<Size> Thumbnails { get; set; }

		public ImageFileHandler(IFileUploader fileUploader, string filePath, FilePathGenerator newPathGenerator, IList<Size> thumbnails) 
			: base(fileUploader, filePath, newPathGenerator)
		{
			LoadBitmapFromFileStream();

			Thumbnails = thumbnails;
		}

		public ImageFileHandler(IFileUploader fileUploader, Stream fileStream, string fileName, FilePathGenerator newPathGenerator, IList<Size> thumbnails)
			: base(fileUploader, fileStream, fileName, newPathGenerator)
		{
			LoadBitmapFromFileStream();

			Thumbnails = thumbnails;
		}

		private void LoadBitmapFromFileStream()
		{
			_bitmap = new Bitmap(FileStream);
		}

		public ImageFileHandler Resize(int width, int height, bool keepProportions, bool biggerIfNeeded = false)
		{
			_bitmap = _bitmap.Resize(width, height, keepProportions, biggerIfNeeded);

			return this;
		}

		public ImageFileHandler Crop(int width, int height)
		{
			_bitmap = _bitmap.Crop(width, height);

			return this;
		}

		public long GetCurrentSizeInKbs()
		{
			return (_bitmap.ToStream(ImageFormat.Jpeg).Length / 1024);
		}

		public override string UploadFile()
		{
			// Do iPhone fix
            ImageHelpers.FixExifRotatation(_bitmap);

			// Saves the edited image to the stream
			FileStream = _bitmap.ToStream(ImageFormat.Jpeg);

			var currentDate = DateTime.UtcNow;

			// Creates the file upload tasks
			var tasks = new List<FileUploadTask>();

			// Goes over all thumbnail sizes and created them
			if (Thumbnails != null)
			{
				foreach (var currThumbnail in Thumbnails)
				{
					var currStream = _bitmap.Resize(currThumbnail.Width, currThumbnail.Height, true, true).ToStream(ImageFormat.Jpeg);

					tasks.Add(new FileUploadTask(currStream, NewFilePathGenerator.GeneratePath(FileName, currThumbnail.Width + "x" + currThumbnail.Height, false, currentDate)));
				}
			}

			// Adds the main image
			tasks.Add(new FileUploadTask(FileStream, NewFilePathGenerator.GeneratePath(FileName, createDate: currentDate)));

			// Uploads the file using the uploader and returns the relative path
			var resultPaths = FileUploader.UploadFiles(tasks);

			return ((resultPaths.Count > 0) ? resultPaths[resultPaths.Count - 1] : null);
		}
	}
}
