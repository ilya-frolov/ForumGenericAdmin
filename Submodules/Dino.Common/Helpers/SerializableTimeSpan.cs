using System;

namespace Dino.Common.Helpers
{
    public class SerializableTimeSpan
    {
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }

        public SerializableTimeSpan()
        {
        }

        public SerializableTimeSpan(TimeSpan timeSpan)
        {
            Hours = (int) Math.Floor(timeSpan.TotalHours);
            Minutes = timeSpan.Minutes;
            Seconds = timeSpan.Seconds;
        }

        public SerializableTimeSpan(DateTime dateTime) : this(dateTime.TimeOfDay)
        {
        }

        public TimeSpan ToTimeSpan()
        {
            return new TimeSpan(Hours, Minutes, Seconds);
        }
    }
}
