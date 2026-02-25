using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dino.Common.Comparers;

namespace Dino.Common.Helpers
{
	public static class ListHelpers
	{
		public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
		{
			return ((list == null) || !list.Any());
		}

		public static bool IsNotNullOrEmpty<T>(this IEnumerable<T> list)
		{
			return (!list.IsNullOrEmpty());
		}

		public static void Foreach<T>(this IEnumerable<T> enumeration, Action<T> action)
		{
			foreach (var currVal in enumeration)
			{
				action(currVal);
			}
		}

		public static void AddRange<T>(this ISet<T> set, IEnumerable<T> range)
		{
			foreach (var currItem in range)
			{
				set.Add(currItem);
			}
		}

		public static void RemoveRange<T>(this ISet<T> set, IEnumerable<T> range)
		{
			foreach (var currItem in range)
			{
				if (set.Contains(currItem))
				{
					set.Remove(currItem);
				}
			}
		}

		public static ICollection<T> CloneCollection<T>(this ICollection<T> list)
		{
			var newList = (ICollection<T>)Activator.CreateInstance(list.GetType());

			foreach (var currItem in list)
			{
				newList.Add(currItem);
			}

			return newList;
		}

		public static List<object> ToList(this IEnumerable enumerable)
		{
			if (enumerable == null)
			{
				return new List<object>();
			}

			return Enumerable.ToList(enumerable.Cast<object>());
		}

		public static List<T> ToSingleItemList<T>(this T obj)
		{
			return new List<T>
			{
				obj
			};
		}

		public static List<T> And<T>(this T firstItem, T secondItem)
		{
			return new List<T>
			{
				firstItem,
				secondItem
			};
		}

		public static List<T> And<T>(this T firstItem, IEnumerable<T> itemsToAdd)
		{
			var list = new List<T>
			{
				firstItem
			};

			list.AddRange(itemsToAdd);

			return list;
		}

		public static ICollection<T> And<T>(this ICollection<T> list, T item)
		{
			list.Add(item);

			return list;
		}

		public static ICollection<T> And<T>(this ICollection<T> list, IEnumerable<T> itemsToAdd)
		{
			itemsToAdd.Foreach(list.Add);

			return list;
		}

	    public static bool ContainsAny<T>(this IEnumerable<T> list, params T[] items)
	    {
	        var collection = list as ICollection<T>;
	        if (collection == null)
	        {
	            collection = list.ToList<T>();
	        }

	        return items.Any(list.Contains);
	    }

        public static List<TResult> SelectList<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
	    {
	        return source.Select(selector).ToList<TResult>();
	    }

        public static async Task<List<TResult>> SelectListAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> selector)
        {
            var list = new List<TResult>();

            foreach (var currItem in source)
            {
                list.Add(await selector(currItem));
            }

            return list;
        }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
#else
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return new HashSet<T>();
            }

            return new HashSet<T>(enumerable);
        }
#endif

        public static HashSet<TResult> SelectHashSet<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return source.Select(selector).ToHashSet();
        }

        /// <summary>Determines whether all elements of a sequence satisfy a condition.</summary>
        /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the elements to apply the predicate to.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <returns>
        /// <see langword="true" /> if every element of the source sequence passes the test in the specified predicate, or if the sequence is empty; otherwise, <see langword="false" />.</returns>
        public static async Task<bool> AllAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, Task<bool>> predicate)
        {
            foreach (TSource source1 in source)
            {
                if (!(await predicate(source1)))
                    return false;
            }
            return true;
        }

        public static async Task<bool> AnyAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, Task<bool>> predicate)
        {
            foreach (TSource element in source)
            {
                if (await predicate(element))
                {
                    return true;
                }
            }

            return false;
        }

        public static void Sort<TSource, T>(this List<TSource> source, Expression<Func<TSource, T>> member)
        {
            source.Sort((source1, source2) =>
            {
                var value1 = member.Compile().Invoke(source1);
                var value2 = member.Compile().Invoke(source2);

                return Comparer<T>.Default.Compare(value1, value2);
            });
        }

        public static void SortDescending<TSource, T>(this List<TSource> source, Expression<Func<TSource, T>> member)
        {
            source.Sort((source1, source2) =>
            {
                var value1 = member.Compile().Invoke(source1);
                var value2 = member.Compile().Invoke(source2);

                return (Comparer<T>.Default.Compare(value1, value2) * -1);
            });
        }
    }
}
