using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVSeriesNotifications.Notifications
{
    public interface INotificationService
    {
        Task NotifyAboutErrors(string message);

        Task NotifyAboutNewSeason(string tvShow, int newSeason);
    }
}
