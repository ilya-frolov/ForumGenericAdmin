using System;
using System.Globalization;
using Dino.Common.Culture;

namespace Dino.Common.Helpers
{
	public static class DateTimeHelper
	{
		/// <summary>
		/// Converts to a byte array by the DateTime object Ticks.
		/// </summary>
		/// <param name="dateTime">DateTime object to convert</param>
		/// <returns>Byte array conversion of the DateTime object</returns>
		public static byte[] ToByteArray(this DateTime dateTime)
		{
			return BitConverter.GetBytes(dateTime.Ticks);
		}

		/// <summary>
		/// Returns a DateTime object floored up to it's seconds value.
		/// </summary>
		/// <param name="dateTime">DateTime object to fix</param>
		/// <returns>A DateTime object floored up to it's seconds value</returns>
		public static DateTime ToCleanSeconds(this DateTime dateTime)
		{
			return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 
								dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);
		}

		/// <summary>
		/// Returns a DateTimeOffset object floored up to it's seconds value.
		/// </summary>
		/// <param name="dateTime">DateTimeOffset object to fix</param>
		/// <returns>A DateTimeOffset object floored up to it's seconds value</returns>
		public static DateTimeOffset ToCleanSeconds(this DateTimeOffset dateTime)
		{
			return new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day,
										dateTime.Hour, dateTime.Minute, dateTime.Second, 
										dateTime.Millisecond, dateTime.Offset);
		}

		/// <summary>
		/// Converts to a byte array by the DateTimeOffset object Ticks.
		/// </summary>
		/// <param name="dateTime">DateTimeOffset object to convert</param>
		/// <returns>Byte array conversion of the DateTimeOffset object</returns>
		public static byte[] ToByteArray(this DateTimeOffset dateTime)
		{
			return BitConverter.GetBytes(dateTime.Ticks);
		}

		/// <summary>
		/// Converts DateTime object to the a DateTimeOffset with the specific time zone. The date and time themself doesn't change.
		/// </summary>
		/// <param name="date">The DateTime to convert</param>
		/// <param name="timeZone">The time zone</param>
		/// <returns>The DateTimeOffset object with the specified time zone</returns>
		public static DateTimeOffset ToDateTimeOffset(this DateTime date, float timeZone)
		{
			var offset = timeZone.ToHoursTimeSpan();

		    if (date.Kind != DateTimeKind.Unspecified)
		    {
		        date = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
		    }

		    return new DateTimeOffset(date, offset);
		}

		/// <summary>
		/// Converts DateTimeOffset object to the specified time zone
		/// </summary>
		/// <param name="date">The DateTimeOffset to convert</param>
		/// <param name="timeZone">The time zone</param>
		/// <returns>The DateTimeOffset object with the specified time zone</returns>
		public static DateTimeOffset ToTimeZone(this DateTimeOffset date, float timeZone)
		{
			var offset = timeZone.ToHoursTimeSpan();

			return date.ToOffset(offset);
		}

		/// <summary>
		/// Converts a DateTime object to it's javascript string representation.
		/// </summary>
		/// <param name="dateTime">The DateTime to convert.</param>
		/// <returns>The javascript string representation.</returns>
		public static string ToJavaScriptString(this DateTime dateTime)
		{
			return dateTime.ToString("s");
		}

		/// <summary>
		/// Converts a DateTime? object to it's javascript string representation.
		/// </summary>
		/// <param name="dateTime">The DateTime? to convert.</param>
		/// <returns>The javascript string representation.</returns>
		public static string ToJavaScriptString(this DateTime? dateTime)
		{
			string str = null;

			if (dateTime.HasValue)
			{
				str = dateTime.Value.ToJavaScriptString();
			}

			return str;
		}

		/// <summary>
		/// Converts a DateTimeOffset object to it's javascript string representation.
		/// </summary>
		/// <param name="dateTime">The DateTimeOffset to convert.</param>
		/// <returns>The javascript string representation.</returns>
		public static string ToJavaScriptString(this DateTimeOffset dateTime)
		{
			return dateTime.UtcDateTime.ToJavaScriptString();
		}

		/// <summary>
		/// Converts a DateTimeOffset? object to it's javascript string representation.
		/// </summary>
		/// <param name="dateTime">The DateTimeOffset? to convert.</param>
		/// <returns>The javascript string representation.</returns>
		public static string ToJavaScriptString(this DateTimeOffset? dateTime)
		{
			string str = null;

			if (dateTime.HasValue)
			{
				str = dateTime.Value.ToJavaScriptString();
			}

			return str;
		}

		/// <summary>
		/// Converts system's DayOfWeek enum to Common's WeekDays Enum
		/// </summary>
		/// <param name="day">The DayOfWeek value.</param>
		/// <returns>The corresponding WeekDays value</returns>
		public static WeekDays ToWeekDays(this DayOfWeek day)
		{
			return DateTimeManager.DayOfWeekToWeekDays(day);
		}

		/// <summary>
		/// Converts system's DayOfWeek enum to Common's WeekDays Enum
		/// </summary>
		/// <param name="day">The DayOfWeek value. Null is 'None'.</param>
		/// <returns>The corresponding WeekDays value</returns>
		public static WeekDays ToWeekDays(this DayOfWeek? day)
		{
			return DateTimeManager.DayOfWeekToWeekDays(day);
		}

		public static PartOfDay PartOfDay(this DateTimeOffset dateTime)
		{
			return dateTime.TimeOfDay.PartOfDay();
		}

		public static PartOfDay PartOfDay(this DateTime dateTime)
		{
			return dateTime.TimeOfDay.PartOfDay();
		}

		public static PartOfDay PartOfDay(this TimeSpan timeOfDay)
		{
			PartOfDay result;

			if ((timeOfDay.Hours >= 5) && (timeOfDay.Hours <= 11))
			{
				result = Culture.PartOfDay.Morning;
			}
			else if ((timeOfDay.Hours >= 12) && (timeOfDay.Hours <= 16))
			{
				result = Culture.PartOfDay.Afternoon;
			}
			else if ((timeOfDay.Hours >= 17) && (timeOfDay.Hours <= 21))
			{
				result = Culture.PartOfDay.Evening;
			}
			else
			{
				result = Culture.PartOfDay.Night;
			}

			return result;
		}

	    public static DateTime ToDateTime(this TimeSpan timeSpan)
	    {
	        var now = DateTime.Now;

	        return new DateTime(now.Year, now.Month, now.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds, DateTimeKind.Utc);
	    }

		public static string GetMonthName(this DateTime? dateTime)
		{
			var month = String.Empty;
			if (dateTime.HasValue)
			{
				month = dateTime.Value.GetMonthName();
			}

			return month;
		}

		public static string GetMonthName(this DateTime dateTime)
		{
			return CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(dateTime.Month);
		}

		public static bool HasPassed(this DateTimeOffset dateTime)
		{
			var currDate = DateTimeOffset.Now;
			return currDate > dateTime;
		}

		public static bool HasPassed(this DateTime dateTime)
		{
			var currDate = DateTime.Now;
			return currDate > dateTime;
		}

		public static bool HasPassedUtc(this DateTimeOffset dateTime)
		{
			var currDate = DateTimeOffset.UtcNow;
			return currDate > dateTime;
		}

		public static bool HasPassedUtc(this DateTime dateTime)
		{
			var currDate = DateTime.UtcNow;
			return currDate > dateTime;
		}

        public static int GetMonthsBetween(DateTime from, DateTime to)
        {
            if (from > to) return GetMonthsBetween(to, from);

            var monthDiff = Math.Abs((to.Year * 12 + (to.Month - 1)) - (from.Year * 12 + (from.Month - 1)));

            if (from.AddMonths(monthDiff) > to || to.Day < from.Day)
            {
                return monthDiff - 1;
            }

            return monthDiff;
        }

        public static DateTime GetStartOfWeek(DateTime date)
        {
            return date.Subtract(TimeSpan.FromDays((int)date.DayOfWeek));
        }

        public static DateTime GetEndOfWeek(DateTime date)
        {
            return date.AddDays(6 - (int)date.DayOfWeek);
        }
    }
}
