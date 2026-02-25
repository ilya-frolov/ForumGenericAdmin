using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Dino.Mvc.Common.Helpers
{
	public static class HtmlHelperExtensions
	{
		public static IHtmlContent EnumRadioButtonFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
			Expression<Func<TModel, TProperty>> expression, Type enumType)
		{
			if (!enumType.IsEnum)
			{
				throw new Exception("Type passed is not an enum type.");
			}

			var returnString = String.Empty;

			foreach (var currVal in Enum.GetValues(enumType))
			{
				returnString += htmlHelper.RadioButtonFor(expression, currVal, (IDictionary<string, object>) null) + " " +
				                Enum.GetName(enumType, currVal) + "<br />";
			}

			return new HtmlString(returnString);
		}

		/// <summary>
		/// Returns a text email input element for each property in the object that is represented by the specified expression, using the specified HTML attributes.
		/// </summary>
		/// 
		/// <returns>
		/// An HTML input element type attribute is set to "email" for each property in the object that is represented by the expression.
		/// </returns>
		/// <param name="htmlHelper">The HTML helper instance that this method extends.</param><param name="expression">An expression that identifies the object that contains the properties to render.</param><param name="htmlAttributes">A dictionary that contains the HTML attributes to set for the element.</param><typeparam name="TModel">The type of the model.</typeparam><typeparam name="TProperty">The type of the value.</typeparam><exception cref="T:System.ArgumentException">The <paramref name="expression"/> parameter is null or empty.</exception>
		public static IHtmlContent EmailFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
			Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes)
		{
			if (htmlAttributes == null)
			{
				htmlAttributes = new Dictionary<string, object>();
			}

			htmlAttributes.Add("type", "email");

			return htmlHelper.TextBoxFor(expression, htmlAttributes);
		}

		/// <summary>
		/// Returns a text number input element for each property in the object that is represented by the specified expression, using the specified HTML attributes.
		/// </summary>
		/// 
		/// <returns>
		/// An HTML input element type attribute is set to "number" for each property in the object that is represented by the expression.
		/// </returns>
		/// <param name="htmlHelper">The HTML helper instance that this method extends.</param><param name="expression">An expression that identifies the object that contains the properties to render.</param><param name="htmlAttributes">A dictionary that contains the HTML attributes to set for the element.</param><typeparam name="TModel">The type of the model.</typeparam><typeparam name="TProperty">The type of the value.</typeparam><exception cref="T:System.ArgumentException">The <paramref name="expression"/> parameter is null or empty.</exception>
		public static IHtmlContent NumberFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
			Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes)
		{
			if (htmlAttributes == null)
			{
				htmlAttributes = new Dictionary<string, object>();
			}

			htmlAttributes.Add("type", "number");

			return htmlHelper.TextBoxFor(expression, htmlAttributes);
		}
	}
}