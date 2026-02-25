using System.IO;
using Dino.Infra.Files.Uploaders;

namespace Dino.Infra.Files.Handlers
{
	public class GeneralFileHandler : BaseFileHandler
	{
		public GeneralFileHandler(IFileUploader fileUploader, string filePath, FilePathGenerator newPathGenerator) 
			: base(fileUploader, filePath, newPathGenerator)
		{
		}

		public GeneralFileHandler(IFileUploader fileUploader, Stream fileStream, string fileName, FilePathGenerator newPathGenerator)
			: base(fileUploader, fileStream, fileName, newPathGenerator)
		{
		}
	}
}
