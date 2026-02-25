using System;
using System.IO;
using System.Text;
using Dino.Common.Helpers;

namespace Dino.Infra.Files
{
	public abstract class FilePathGenerator
	{
		private const string SEPARATOR = "_";

		protected bool IsWebPath { get; set; }
		protected string BasePath { get; set; }
		protected string NamePrefix { get; set; }
		protected string[] Ids { get; set; }

		private int _timesGenerated = 0;

		protected FilePathGenerator(bool isWebPath, string basePath, string namePrefix, params string[] ids)
		{
			IsWebPath = isWebPath;
			BasePath = basePath;
			NamePrefix = namePrefix;
			Ids = ids;
		}

		public virtual string GeneratePath(string originalFileName, string suffix = null, bool increment = true, DateTime? createDate = null)
		{
			string finalPath;

			if (!createDate.HasValue)
			{
				createDate = DateTime.UtcNow;
			}

			var currentDateTimeString = createDate.ToString().Replace(" ", String.Empty)
															 .Replace(":", String.Empty)
															 .Replace("/", String.Empty);

			var path = new StringBuilder();
			path.Append(NamePrefix);
			path.Append(SEPARATOR);
			path.Append(String.Join(SEPARATOR, Ids));
			path.Append(SEPARATOR);
			path.Append(currentDateTimeString);
			path.Append(_timesGenerated);

			if (!suffix.IsNullOrEmpty())
			{
				path.Append(SEPARATOR);
				path.Append(suffix);
			}

			path.Append(Path.GetExtension(originalFileName));

			if (increment)
			{
				_timesGenerated++;
			}

			if (!BasePath.IsNullOrEmpty())
			{
				if (IsWebPath)
				{
					finalPath = BasePath + "/" + path;
				}
				else
				{
					finalPath = Path.Combine(BasePath, path.ToString());
				}
			}
			else
			{
				finalPath = path.ToString();
			}

			return finalPath;
		}
	}
}
