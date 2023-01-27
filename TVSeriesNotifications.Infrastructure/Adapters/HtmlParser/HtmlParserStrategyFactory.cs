using TVSeriesNotifications.Core.DateTimeProvider;
using TVSeriesNotifications.Domain.Ports.HtmlParser;

namespace TVSeriesNotifications.Infrastructure.Adapters.HtmlParser
{
    public class HtmlParserStrategyFactory : IHtmlParserStrategyFactory
    {
        public IHtmlParser ResolveParsingStrategy(string tvShowPageContent)
        {
            return new HtmlParserV2(new DateTimeProvider());
        }
    }
}
