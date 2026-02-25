using System.Text.RegularExpressions;

namespace Dino.Common.Helpers
{
	public static class YoutubeHelpers
	{
		/// <summary>
		/// Parses the Youtube's ID from a given URL.
		/// </summary>
		/// <param name="url">The Youtube's movie's URL.</param>
		/// <returns>The parsed ID, or empty string if couldn't parse.</returns>
		public static string ParseYoutubeIdFromUrl(string url)
		{
			//Setup the RegEx Match and give it 
			Match regexMatch = Regex.Match(url, "^(?:https?\\:\\/\\/)?(?:www\\.)?(?:youtu\\.be\\/|youtube\\.com\\/(?:embed\\/|v\\/|watch\\?v\\=))([\\w-]{10,12})(?:$|\\&|\\?\\#).*",
							   RegexOptions.IgnoreCase);

			string value = "";
			if (regexMatch.Success)
			{
				value = regexMatch.Groups[1].Value;
			}
			else if (url.Length == 11)
			{
				// It's probably just the ID, without the URL.
				value = url;
			}

			return value;
		}


		/// <summary>
		/// Gets a Youtube-video's URL from it's ID.
		/// </summary>
		/// <param name="id">The ID of the video.</param>
		/// <returns>The URL of the video.</returns>
		public static string GetYoutubeUrlFromId(string id)
		{
			return ("http://www.youtube.com/v/" + id);
		}
	}
}
