using System.Globalization;

namespace Dino.Common.Culture
{
	/// <summary>
	/// This class represents a language and it's weight.
	/// </summary>
	public class WeightedLanguage
	{
		public string Language { get; set; }
		public double Weight { get; set; }

		public static WeightedLanguage Parse(string weightedLanguageString)
		{
			// de 
			// en;q=0.8 
			var parts = weightedLanguageString.Split(';');
			var result = new WeightedLanguage { Language = parts[0].Trim(), Weight = 1.0 };
			if (parts.Length > 1)
			{
				parts[1] = parts[1].Replace("q=", "").Trim();
				double d;
				if (double.TryParse(parts[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
					result.Weight = d;
			}
			return result;
		}
	}
}
