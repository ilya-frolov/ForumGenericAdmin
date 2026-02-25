using System;
using System.Collections.Generic;

namespace Dino.Common.Helpers
{
	public static class ArrayHelpers
	{
		public static void Foreach<T>(this T[] array, Action<T> action)
		{
			foreach (var currElement in array)
			{
				action(currElement);
			}
		}

		public static T[] ToArray<T>(this T obj)
		{
			return new[] {obj};
		}

        public static T[] SafeClone<T>(this T[] array)
        {
            T[] clone = null;

            if (array != null)
            {
                clone = (T[]) array.Clone();
            }

            return clone;
        }
	}
}
