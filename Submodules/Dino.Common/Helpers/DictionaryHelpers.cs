using System;
using System.Collections.Generic;

namespace Dino.Common.Helpers
{
	public static class DictionaryHelpers
	{
		/// <summary>
		/// Adds a value to the dictionary, if it doesn't exists.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TValue">The type of the value.</typeparam>
		/// <param name="dic">The dictionary to add the value to.</param>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <returns>Was the value added.</returns>
		public static bool AddIfNotExists<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value)
		{
			bool added = false;

			// Add if not exists.
			if (!dic.ContainsKey(key))
			{
				dic.Add(key, value);
				added = true;
			}

			// Return if added.
			return (added);
		}

		/// <summary>
		/// Adds or sets a value to the dictionary.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TValue">The type of the value.</typeparam>
		/// <param name="dic">The dictionary to add the value to.</param>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		public static void AddOrSet<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value)
		{
			// Add if not exists.
			if (!dic.ContainsKey(key))
			{
				dic.Add(key, value);
			}
			else
			{
				dic[key] = value;
			}
		}

		/// <summary>
		/// Gets a value from the dictionary or sets the default value if doesn't exist.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TValue">The type of the value.</typeparam>
		///  <param name="dic">The dictionary to get the value from.</param>
		/// <param name="key">The key.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns></returns>
		public static TValue GetOrSetDefault<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue defaultValue)
		{
			if (!dic.ContainsKey(key))
			{
				dic.Add(key, defaultValue);
			}

			return dic[key];
		}

		public static IDictionary<T1, T2> CloneDictionary<T1, T2>(this IDictionary<T1, T2> dic)
		{
			var newDic = (IDictionary<T1, T2>)Activator.CreateInstance(dic.GetType());

			foreach (var currKeyPair in dic)
			{
				newDic.Add(currKeyPair.Key, currKeyPair.Value);
			}

			return newDic;
		}
	}
}
