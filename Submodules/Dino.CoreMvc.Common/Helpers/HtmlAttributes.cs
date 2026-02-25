using System.Collections.Generic;

namespace Dino.Mvc.Common.Helpers
{
	public class HtmlAttributes : Dictionary<string, object>
	{
		public static HtmlAttributes New()
		{
			return new HtmlAttributes();
		}

		public HtmlAttributes WithCssClass(string cssClass)
		{
			const string key = "class";

			if (ContainsKey(key))
			{
				this[key] = this[key] + " " + cssClass;
			}
			else
			{
				Add(key, cssClass);
			}

			return this;
		}

		public HtmlAttributes WithStyle(string style)
		{
			const string key = "style";

			if (ContainsKey(key))
			{
				this[key] = this[key] + " " + style;
			}
			else
			{
				Add(key, style);
			}

			return this;
		}

		public HtmlAttributes WithPlaceholder(string placeHolderText)
		{
			const string key = "placeholder";

			this[key] = placeHolderText;

			return this;
		}

		public HtmlAttributes WithFormFileUpload()
		{
			const string key = "enctype";

			this[key] = "multipart/form-data";

			return this;
		}

		public HtmlAttributes WithDisabled()
		{
			const string key = "disabled";

			this[key] = "";

			return this;
		}

		public HtmlAttributes WithRequired()
		{
			const string key = "required";

			this[key] = "";

			return this;
		}

		public HtmlAttributes WithCustomAttribute(string attribute, string value)
		{
			this[attribute] = value;

			return this;
		}
	}
}
