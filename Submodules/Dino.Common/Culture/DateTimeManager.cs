using System;
using Dino.Common.Helpers;

namespace Dino.Common.Culture
{
	public static class DateTimeManager
	{
		private static TimeSpan _timeSkip = new TimeSpan(0);

		public static void DoTimeTravel(TimeSpan timeSkip)
		{
			_timeSkip = timeSkip;
		}

		public static TimeSpan GetTimeSkip()
		{
			return _timeSkip;
		}

		/// <summary>
		/// Gets the current date time offset of the specific time zone.
		/// </summary>
		/// <param name="timeZone">Time zone hours.</param>
		/// <returns>Current DateTimeOffset.</returns>
		public static DateTimeOffset GetCurrentDateTime(float timeZone)
		{
			return DateTimeOffset.UtcNow.ToTimeZone(timeZone).Add(_timeSkip);
		}

		/// <summary>
		/// Converts system's DayOfWeek enum to Common's WeekDays Enum
		/// </summary>
		/// <param name="day">The DayOfWeek value. Null is 'None'.</param>
		/// <returns>The correponding WeekDays value</returns>
		public static WeekDays DayOfWeekToWeekDays(DayOfWeek? day)
		{
			var weekDays = WeekDays.None;

			if (day.HasValue)
			{
				switch (day.Value)
				{
					case DayOfWeek.Sunday:
					{
						weekDays = WeekDays.Sunday;
						break;
					}
					case DayOfWeek.Monday:
					{
						weekDays = WeekDays.Monday;
						break;
					}
					case DayOfWeek.Tuesday:
					{
						weekDays = WeekDays.Tuesday;
						break;
					}
					case DayOfWeek.Wednesday:
					{
						weekDays = WeekDays.Wednesday;
						break;
					}
					case DayOfWeek.Thursday:
					{
						weekDays = WeekDays.Thursday;
						break;
					}
					case DayOfWeek.Friday:
					{
						weekDays = WeekDays.Friday;
						break;
					}
					case DayOfWeek.Saturday:
					{
						weekDays = WeekDays.Saturday;
						break;
					}
				}
			}

			return weekDays;
		}
	}
}
