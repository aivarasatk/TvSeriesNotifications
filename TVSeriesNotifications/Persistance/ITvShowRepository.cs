using System.Collections.Generic;
using System.Threading.Tasks;

namespace TVSeriesNotifications.Persistance
{
    public interface ITvShowRepository
    {
        Task<IEnumerable<string>> RetrieveTvShows();
    }
}
