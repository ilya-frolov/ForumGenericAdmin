using System;
using System.Linq.Expressions;

namespace Dino.Common.Helpers
{
	public static class EntityPropertyUpdater
	{
		/// <summary>
		/// Updates specific properties between two entities from the same type
		/// </summary>
		/// <typeparam name="T">Entity type</typeparam>
		/// <param name="source">The source entity that will be updated</param>
		/// <param name="newValues">The entity with the new values</param>
		/// <param name="properties">The properties that will be updated</param>
		/// <returns>The updated entity</returns>
		public static T UpdatePropertiesFrom<T>(this T source, T newValues, 
											params Expression<Func<T, object>>[] properties) where T : class, new()
		{
			var type = typeof(T);

			if (source == null)
			{
				source = new T();
			}

			if (newValues == null)
			{
				source = null;
			}
			else
			{
				foreach (var currExpression in properties)
				{
					// Gets the property name from the expression
					// NOTE - The type check was used because when we have a type that isn't basic (Ex: DateTime) 
					// the expression is from a different type so we need to dig deeper
					string propertyName = null;
					if (currExpression.Body is MemberExpression)
					{
						propertyName = ((MemberExpression) currExpression.Body).Member.Name;
					}
					else if (currExpression.Body is UnaryExpression)
					{
						propertyName = ((MemberExpression) ((UnaryExpression) currExpression.Body).Operand).Member.Name;
					}

					var currProperty = type.GetProperty(propertyName);

					// Checks and updates the values of the property
					var sourceVal = currProperty.GetValue(source);
					var newVal = currProperty.GetValue(newValues);

					if (!object.Equals(sourceVal, newVal))
					{
						currProperty.SetValue(source, newVal);
					}
				}
			}

			return source;
		}
	}
}
