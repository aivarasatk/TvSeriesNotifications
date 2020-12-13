using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVSeriesNotifications.Persistance
{
    public interface ITvShowRepository
    {
        Task<IEnumerable<string>> RetrieveTvShows();
    }
}
