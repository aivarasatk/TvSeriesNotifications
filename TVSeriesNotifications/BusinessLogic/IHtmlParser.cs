using System.Collections.Generic;
using TVSeriesNotifications.DTO;

namespace TVSeriesNotifications.BusinessLogic
{
    public interface IHtmlParser
    {
        IEnumerable<SeasonNode> SeasonNodes(string tvShowPageContent);

        bool ShowIsCancelled(string tvShowPageContent);

        bool AnyEpisodeHasAired(string pageContents);
    }
}
