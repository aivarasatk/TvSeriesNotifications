using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVSeriesNotifications.DTO;

namespace TVSeriesNotifications.BusinessLogic
{
    public interface ISeasonChecker
    {
        Task<(bool newSeasonAired, NewSeason seasonInfo)> TryCheckForNewSeasonAsync(string tvShow);
    }
}
