using System;

namespace TVSeriesNotifications.Core.DateTimeProvider
{
    public interface IDateTimeProvider
    {
        public DateTime Now { get; }
    }
}
