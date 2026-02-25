using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Dino.Common.Helpers
{
	public static class EnumHelpers
	{
		public static Dictionary<int, string> GetEnumDictionary<T>()
		{
			return GetEnumDictionary(typeof (T));
		}

		public static Dictionary<int, string> GetEnumDictionary(Type type)
		{
			if (!type.IsEnum)
			{
				throw new Exception("Not an enum type.");
			}

			var result = new Dictionary<int, string>();
		    var isInt16 = (Enum.GetUnderlyingType(type) == typeof (Int16));

			foreach (var currValue in Enum.GetValues(type))
			{
			    if (isInt16)
			    {
			        var value = (short) currValue;
			        result.Add(value, Enum.GetName(type, currValue));
			    }
			    else
			    {
                    result.Add((int)currValue, Enum.GetName(type, currValue));
                }
			}

			return result;
		}

        public static Dictionary<int, string> GetEnumDictionaryWithDisplayName(Type type)
        {
            if (!type.IsEnum)
            {
                throw new Exception("Not an enum type.");
            }

            var result = new Dictionary<int, string>();
            var isInt16 = Enum.GetUnderlyingType(type) == typeof(Int16);

            foreach (var currValue in Enum.GetValues(type))
            {
                var enumValue = (Enum)currValue;
                var displayName = enumValue.GetDisplayName();

                if (isInt16)
                {
                    var value = (short)currValue;
                    result.Add(value, displayName);
                }
                else
                {
                    result.Add((int)currValue, displayName);
                }
            }

            return result;
        }

        public static string GetDisplayName(this Enum value)
        {
            return value.GetType()
                .GetMember(value.ToString()).FirstOrDefault()
                ?.GetCustomAttribute<DisplayAttribute>(false)
                ?.GetName() ?? value.ToString();
        }
    }
}
