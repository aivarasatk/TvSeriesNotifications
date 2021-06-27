using System.Collections.Generic;

namespace TVSeriesNotifications.BusinessLogic
{
    public interface IHtmlParser
    {
        IEnumerable<int> Seasons(string tvShowPageContent);

        bool ShowIsCancelled(string tvShowPageContent);

        bool AnyEpisodeHasAired(string pageContents);
    }
}
