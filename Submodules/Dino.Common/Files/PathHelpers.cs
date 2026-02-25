using System;
using System.IO;

namespace Dino.Common.Files
{
	public static class PathHelpers
	{
		/// <summary>
		/// Adds a prefix to a filename in a path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="prefix">The prefix.</param>
		/// <returns>The new path with the updated filename.</returns>
		public static string AddPrefixToFilename(string path, string prefix)
		{
			string newPath = "";
			if (!String.IsNullOrEmpty(path))
			{
				newPath = Path.Combine(Path.GetDirectoryName(path), (prefix + Path.GetFileName(path)));
			}

			return newPath;
		}

		public static string AddSuffixToFilename(string filename, string suffix)
		{
			string fDir = Path.GetDirectoryName(filename);
			string fName = Path.GetFileNameWithoutExtension(filename);
			string fExt = Path.GetExtension(filename);
			return Path.Combine(fDir, String.Concat(fName, suffix, fExt));
		}

	}
}