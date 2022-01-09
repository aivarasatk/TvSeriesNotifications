using System;

namespace TVSeriesNotifications.Core.DateTimeProvider
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
