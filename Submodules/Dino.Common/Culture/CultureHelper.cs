using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

namespace Dino.Common.Culture
{
	public class CultureHelper
	{
		#region Find user's culture methods

		/// <summary>
		/// Gets the culture that we should represent to the user.
		/// </summary>
		/// <param name="headers">The header of the user's request.</param>
		/// <param name="supportedCultures">The supported cultures in the system.</param>
		/// <returns>The culture to represent to the user.</returns>
		public static CultureInfo GetUserCulture(NameValueCollection headers, CultureInfo[] supportedCultures)
		{
			var acceptedCultures = GetUserCultures(headers["Accept-Language"]);
			var culture = GetMatchingCulture(acceptedCultures, supportedCultures);
			return culture;
		}

		/// <summary>
		/// Gets the user's cultures.
		/// </summary>
		/// <param name="acceptLanguage">The accepted-language header.</param>
		/// <returns>The user's cultures.</returns>
		private static CultureInfo[] GetUserCultures(string acceptLanguage) 
		{ 
			// Accept-Language: fr-FR , en;q=0.8 , en-us;q=0.5 , de;q=0.3 
			if (string.IsNullOrWhiteSpace(acceptLanguage)) 
				return new CultureInfo[] { }; 
     
			var cultures = acceptLanguage 
				.Split(',') 
				.Select(s => WeightedLanguage.Parse(s)) 
				.OrderByDescending(w => w.Weight) 
				 .Select(w => GetCultureInfo(w.Language)) 
				 .Where(ci => ci != null) 
				 .ToArray(); 
			return cultures; 
		}

		/// <summary>
		/// Get's the culture information of a specific language.
		/// </summary>
		/// <param name="language">The language.</param>
		/// <returns>The calture information as a CultureInfo object.</returns>
		private static CultureInfo GetCultureInfo(string language) 
		{ 
			try 
			{ 
				return CultureInfo.GetCultureInfo(language); 
			} 
			catch (CultureNotFoundException) 
			{ 
				return null; 
			} 
		}

		#endregion

		#region Matching methods

		/// <summary>
		/// Gets the matching culture from the accepted cultures (by the user), according to the supported cultures..
		/// </summary>
		/// <param name="acceptedCultures">The accepted cultures (by the user).</param>
		/// <param name="supportedCultures">The suppoert cultures.</param>
		/// <returns>The matching culture.</returns>
		private static CultureInfo GetMatchingCulture(CultureInfo[] acceptedCultures, CultureInfo[] supportedCultures)
		{
			return
				// first pass: exact matches as well as requested neutral matching supported region 
				// supported: en-US, de-DE 
				// requested: de, en-US;q=0.8 
				// => de-DE! (de has precendence over en-US) 
				GetMatch(acceptedCultures, supportedCultures, MatchesCompletely)
				// second pass: look for requested neutral matching supported _neutral_ region 
				// supported: en-US, de-DE 
				// requested: de-AT, en-GB;q=0.8 
				// => de-DE! (no exact match, but de-AT has better fit than en-GB) 
				?? GetMatch(acceptedCultures, supportedCultures, MatchesPartly);
		}

		/// <summary>
		/// Gets a match between caltures.
		/// </summary>
		/// <param name="acceptedCultures">The accepted cultures (by the user).</param>
		/// <param name="supportedCultures">The suppoert cultures.</param>
		/// <param name="predicate">The method to use for the matching.</param>
		/// <returns>The matching culture.</returns>
		private static CultureInfo GetMatch(
			CultureInfo[] acceptedCultures,
			CultureInfo[] supportedCultures,
			Func<CultureInfo, CultureInfo, bool> predicate)
		{
			foreach (var acceptedCulture in acceptedCultures)
			{
				var match = supportedCultures.FirstOrDefault(supportedCulture => predicate(acceptedCulture, supportedCulture));
				if (match != null)
					return match;
			}
			return null;
		}

		/// <summary>
		/// Checks for a complete match between caltures.
		/// </summary>
		/// <param name="acceptedCulture">The accepted culture.</param>
		/// <param name="supportedCulture">The suppoert culture.</param>
		/// <returns>Is there a match.</returns>
		private static bool MatchesCompletely(CultureInfo acceptedCulture, CultureInfo supportedCulture)
		{
			if (supportedCulture.Name == acceptedCulture.Name)
				return true;
			// acceptedCulture could be neutral and supportedCulture specific, but this is still a match (de matches de-DE, de-AT, …) 
			if (acceptedCulture.IsNeutralCulture)
			{
				if (supportedCulture.Parent.Name == acceptedCulture.Name)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Checks for a partial match between caltures.
		/// </summary>
		/// <param name="acceptedCulture">The accepted culture.</param>
		/// <param name="supportedCulture">The suppoert culture.</param>
		/// <returns>Is there a match.</returns>
		static bool MatchesPartly(CultureInfo acceptedCulture, CultureInfo supportedCulture)
		{
			supportedCulture = supportedCulture.Parent;
			if (!acceptedCulture.IsNeutralCulture)
				acceptedCulture = acceptedCulture.Parent;

			if (supportedCulture.Name == acceptedCulture.Name)
				return true;
			return false;
		}

		#endregion
	}
}
