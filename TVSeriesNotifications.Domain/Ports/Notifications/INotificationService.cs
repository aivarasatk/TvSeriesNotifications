using TVSeriesNotifications.Domain.Models;

namespace TVSeriesNotifications.Domain.Ports.Notifications
{
    public interface INotificationService
    {
        void NotifyAboutErrors(string message);

        void NotifyNewSeason(NewSeason season);
    }
}
