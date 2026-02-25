using System;

namespace Dino.Common.Helpers
{
	public static class NumericHelpers
	{
		public static TimeSpan ToHoursTimeSpan(this float num)
		{
			var hours = (int)num;
			var minutes = (int)((num - hours) * 60);
			return new TimeSpan(hours, minutes, 0);
		}

		public static TimeSpan Milliseconds(this int milliseconds)
		{
			return new TimeSpan(0, 0, 0, 0, milliseconds);
		}

		public static TimeSpan Seconds(this int seconds)
		{
			return new TimeSpan(0, 0, seconds);
		}

		public static TimeSpan Minutes(this int minutes)
		{
			return new TimeSpan(0, minutes, 0);
		}

		public static TimeSpan Hours(this int hours)
		{
			return new TimeSpan(hours, 0, 0);
		}

		public static TimeSpan Days(this int days)
		{
			return new TimeSpan(days, 0, 0, 0);
		}

		public static TimeSpan Weeks(this int weeks)
		{
			return new TimeSpan(weeks * 7, 0, 0, 0);
		}
	}
}
