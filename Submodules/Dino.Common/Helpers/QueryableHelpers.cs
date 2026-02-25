using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dino.Common.Helpers
{
	public static class QueryableHelpers
	{
        /// <summary>
        /// Selects a property from a generic type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="source">The IQueryable to select from.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value to find.</param>
        /// <returns>The list of items that fits the query.</returns>
        public static IQueryable<T> SelectByPropertyName<T>(this IQueryable<T> source, string propertyName, object value) where T : class
        {
            var parameterExpression = Expression.Parameter(typeof(T), "object");
            var propertyOrFieldExpression = Expression.PropertyOrField(parameterExpression, propertyName);

            // Get real type, even if nullable, of the object.
            var type = value.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            var equalityExpression = Expression.Equal(propertyOrFieldExpression, Expression.Convert(Expression.Constant(value, type), propertyOrFieldExpression.Type));
            var lambdaExpression = Expression.Lambda<Func<T, bool>>(equalityExpression, parameterExpression);

            IQueryable<T> query = source.Where(lambdaExpression);
            return query;
        }

        public static IOrderedQueryable<T> OrderByPropertyName<T>(this IQueryable<T> source, string propertyName, bool ascending = true)
        {
            var type = typeof(T);
            var parameter = Expression.Parameter(type, "p");
            PropertyInfo property;
            Expression propertyAccess;
            if (propertyName.Contains('.'))
            {
                // support to be sorted on child fields.
                String[] childProperties = propertyName.Split('.');
                property = type.GetProperty(childProperties[0]);
                propertyAccess = Expression.MakeMemberAccess(parameter, property);
                for (int i = 1; i < childProperties.Length; i++)
                {
                    property = property.PropertyType.GetProperty(childProperties[i]);
                    propertyAccess = Expression.MakeMemberAccess(propertyAccess, property);
                }
            }
            else
            {
                property = typeof(T).GetProperty(propertyName);
                propertyAccess = Expression.MakeMemberAccess(parameter, property);
            }
            var orderByExp = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable),
                                                             ascending ? "OrderBy" : "OrderByDescending",
                                                             new[] { type, property.PropertyType }, source.Expression,
                                                             Expression.Quote(orderByExp));

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(resultExp);
        }

        public static IOrderedQueryable<T> OrderByDescendingPropertyName<T>(this IQueryable<T> source, string propertyName)
            where T : class
        {
            return source.OrderByPropertyName(propertyName, false);
        }

        public static IOrderedQueryable<T> ThenByPropertyName<T>(this IOrderedQueryable<T> source, string propertyName, bool ascending = true)
            where T : class
        {
            var type = typeof(T);
            var parameter = Expression.Parameter(type, "p");
            PropertyInfo property;
            Expression propertyAccess;
            if (propertyName.Contains('.'))
            {
                // support to be sorted on child fields.
                String[] childProperties = propertyName.Split('.');
                property = type.GetProperty(childProperties[0]);
                propertyAccess = Expression.MakeMemberAccess(parameter, property);
                for (int i = 1; i < childProperties.Length; i++)
                {
                    property = property.PropertyType.GetProperty(childProperties[i]);
                    propertyAccess = Expression.MakeMemberAccess(propertyAccess, property);
                }
            }
            else
            {
                property = typeof(T).GetProperty(propertyName);
                propertyAccess = Expression.MakeMemberAccess(parameter, property);
            }
            var orderByExp = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable),
                                                             ascending ? "ThenBy" : "ThenByDescending",
                                                             new[] { type, property.PropertyType }, source.Expression,
                                                             Expression.Quote(orderByExp));

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(resultExp);
        }

        public static IOrderedQueryable<T> ThenByDescendingPropertyName<T>(this IOrderedQueryable<T> source, string propertyName)
            where T : class
        {
            return source.ThenByPropertyName(propertyName, false);
        }
    }

	public interface IOrderByExpression<T>
	{
		IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query);
		IOrderedQueryable<T> ApplyOrderByDescending(IQueryable<T> query);
	}

	public class OrderByExpression<TType, TKey> : IOrderByExpression<TType>
	{
		public Expression<Func<TType, TKey>> Expression { get; private set; }

		public OrderByExpression(Expression<Func<TType, TKey>> expression)
		{
			Expression = expression;
		}

		public IOrderedQueryable<TType> ApplyOrderBy(IQueryable<TType> query)
		{
			return query.OrderBy(Expression);
		}

		public IOrderedQueryable<TType> ApplyOrderByDescending(IQueryable<TType> query)
		{
			return query.OrderByDescending(Expression);
		}
	}
}
