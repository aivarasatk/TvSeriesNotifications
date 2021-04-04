using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace TVSeriesNotifications.BusinessLogic
{
    public interface IHtmlParser
    {
        IEnumerable<HtmlNode> SeasonNodes(string tvShowPageContent);

        bool ShowIsCancelled(string tvShowPageContent);

        bool AnyEpisodeHasAired(string pageContents);
    }
}
