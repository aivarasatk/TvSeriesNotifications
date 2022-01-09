using System.Threading.Tasks;
using TVSeriesNotifications.Domain.Models;

namespace TVSeriesNotifications.BusinessLogic
{
    public interface ISeasonChecker
    {
        Task<(bool newSeasonAired, NewSeason seasonInfo)> TryCheckForNewSeasonAsync(string tvShow);
    }
}
