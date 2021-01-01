using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVSeriesNotifications.DTO;

namespace TVSeriesNotifications.Notifications
{
    public interface INotificationService
    {
        void NotifyAboutErrors(string message);

        void NotifyNewSeason(NewSeason season);
    }
}
