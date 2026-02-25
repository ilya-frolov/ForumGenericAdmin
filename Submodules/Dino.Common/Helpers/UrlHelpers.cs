using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dino.Common.Helpers
{
	public static class UrlHelpers
	{
		public static string Combine(params string[] parts)
		{
			var url = "";

			for (var i = 0; i < parts.Length; i++)
			{
				var currPart = parts[i];

				if (currPart.IsNotNullOrEmpty())
				{
					if (i > 0)
					{
						if (currPart[0] == '/')
						{
							currPart = currPart.Substring(1);
						}
					}

					url += currPart;

					if ((i < (parts.Length - 1)) && (url.Last() != '/'))
					{
						url += "/";
					}
				}
			}

			url = url.Replace("\\", "/");

			return url;
		}
	}
}
