using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dino.Mvc.Common.Helpers
{
	public static class WebHelpers
	{
		/// <summary>
		/// Converts an enumarable to a collection of SelectListItems for use in Drop Down Lists
		/// </summary>
		/// <typeparam name="T">Any class</typeparam>
		/// <param name="list">A collection of the class to convert</param>
		/// <param name="textProperty">The property name to be used for the text field.</param>
		/// <param name="valueProperty">The property name to be used for the value field.</param>
		/// <param name="addEmptyCell">Do we want to add an empty cell.</param>
		/// <param name="emptyCellText">The text of the empty cell, if set to true.</param>
		/// <returns>A collection of SelectListItems for use in Drop Down Lists</returns>
		public static IEnumerable<SelectListItem> ToSelectListItemCollection<T>(this IEnumerable<T> list, string textProperty, string valueProperty,
			bool addEmptyCell = false, string emptyCellText = "")
			where T : class
		{
			var selectListItemCollection = new List<SelectListItem>();

			if (addEmptyCell)
			{
				selectListItemCollection.Add(new SelectListItem
				{
					Value = "",
					Text = emptyCellText
				});
			}

			foreach (var currItem in list)
			{
				var newListItem = new SelectListItem();

				newListItem.Text = ((PropertyInfo)currItem.GetType().GetMember(textProperty)[0]).GetValue(currItem).ToString();
				newListItem.Value = ((PropertyInfo)currItem.GetType().GetMember(valueProperty)[0]).GetValue(currItem).ToString();

				selectListItemCollection.Add(newListItem);
			}

			return selectListItemCollection;
		}

		/// <summary>
		/// Converts a dictionary to a collection of SelectListItems for use in Drop Down Lists where the key is the value and the value is the text.
		/// </summary>
		/// <typeparam name="TKey">Any object.</typeparam>
		/// <typeparam name="TValue">Any object.</typeparam>
		/// <param name="dic"></param>
		/// <param name="addEmptyCell">Do we want to add an empty cell.</param>
		/// <param name="emptyCellText">The text of the empty cell, if set to true.</param>
		/// <returns>A collection of SelectListItems for use in Drop Down Lists</returns>
		public static IEnumerable<SelectListItem> ToSelectListItemCollection<TKey, TValue>(this IDictionary<TKey, TValue> dic, bool addEmptyCell = false, string emptyCellText = "")
		{
			var selectListItemCollection = new List<SelectListItem>();

			if (addEmptyCell)
			{
				selectListItemCollection.Add(new SelectListItem
				{
					Value = "",
					Text = emptyCellText
				});
			}

			foreach (var currItem in dic)
			{
				var newListItem = new SelectListItem();

				newListItem.Text = currItem.Value.ToString();
				newListItem.Value = currItem.Key.ToString();

				selectListItemCollection.Add(newListItem);
			}

			return selectListItemCollection;
		}

		/// <summary>
		/// Converts an enum to a collection of SelectListItems for use in Drop Down Lists
		/// </summary>
		/// <typeparam name="TEnum">An enum</typeparam>
		/// <param name="addEmptyCell">Should an empty value be inserted at the start of the collection</param>
		/// <returns>A collection of SelectListItems for use in Drop Down Lists</returns>
		public static IEnumerable<SelectListItem> EnumToSelectListItemCollection<TEnum>(bool addEmptyCell = false)
			where TEnum : struct, IConvertible
		{
			var enumType = typeof(TEnum);

			if (!enumType.IsEnum)
			{
				throw new InvalidCastException("Type is not an enum.");
			}

			var selectListItemCollection = new List<SelectListItem>();

			if (addEmptyCell)
			{
				selectListItemCollection.Add(new SelectListItem());
			}

			foreach (var currValue in Enum.GetValues(enumType))
			{
				var newListItem = new SelectListItem
				{
					Value = currValue.ToString(),
					Text = Enum.GetName(enumType, currValue)
				};

				selectListItemCollection.Add(newListItem);
			}

			return selectListItemCollection;
		}

		public static object ToIdRouteValues<T>(this T id)
		{
			return new { Id = id };
		}
	}
}