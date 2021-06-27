using TVSeriesNotifications.BusinessLogic;

namespace TVSeriesNotifications.Common
{
    public interface IHtmlParserStrategyFactory
    {
        IHtmlParser ResolveParsingStrategy(string tvShowPageContent);
    }
}