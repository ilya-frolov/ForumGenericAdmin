using System;

namespace Dino.Common.Culture
{
    public class DateTimeRange
    {
        public DateTimeRange(DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;

            if (startDate > endDate)
            {
                throw new ArgumentException("Start Date can't be later then the End Date");
            }
        }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public TimeSpan GetTimeBetween()
        {
            return EndDate.Subtract(StartDate);
        }

        public bool IsDateBetween(DateTime date)
        {
            return ((StartDate <= date) && (date <= EndDate));
        }

        public bool IsWeekDayBetween(DayOfWeek dayOfWeek)
        {
            // Iterate through all the dates between StartDate and EndDate
            for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
            {
                // Check if the day of the week matches the input dayOfWeek
                if (date.DayOfWeek == dayOfWeek)
                {
                    return true;
                }
            }

            // Return false if the dayOfWeek is not found between the range
            return false;
        }

        public bool IsOverlapping(DateTimeRange range)
        {
            // Check if this range's EndDate is before the other range's StartDate or
            // if this range's StartDate is after the other range's EndDate.
            // If either of these conditions is true, there is no overlap.
            if ((EndDate < range.StartDate) || (StartDate > range.EndDate))
            {
                return false;
            }

            // If none of the above conditions are met, the ranges overlap.
            return true;
        }
    }
}
