using System;

namespace TVSeriesNotifications.DateTimeProvider
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
