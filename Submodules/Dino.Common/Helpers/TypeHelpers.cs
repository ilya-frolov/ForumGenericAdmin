using System;

namespace Dino.Common.Helpers
{
	public static class TypeHelpers
	{
		public static Type GetTopBaseType(this Type type)
		{
			var returnType = type;

			if (!IsBaseType(type.BaseType))
			{
				returnType = GetTopBaseType(type.BaseType);
			}

			return returnType;
		}

		public static bool IsBaseType(this Type type)
		{
			return (type.BaseType == typeof(object));
		}
	}
}
