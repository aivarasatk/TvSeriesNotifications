using System;

namespace TVSeriesNotifications.DateTimeProvider
{
    public interface IDateTimeProvider
    {
        public DateTime Now { get; }
    }
}
