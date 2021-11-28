namespace TVSeriesNotifications.Domain.Ports.HtmlParser
{
    public interface IHtmlParserStrategyFactory
    {
        IHtmlParser ResolveParsingStrategy(string tvShowPageContent);
    }
}