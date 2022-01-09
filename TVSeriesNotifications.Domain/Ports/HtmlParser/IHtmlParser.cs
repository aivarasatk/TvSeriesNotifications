namespace TVSeriesNotifications.Domain.Ports.HtmlParser
{
    public interface IHtmlParser
    {
        IEnumerable<int> Seasons(string tvShowPageContent);

        bool ShowIsCancelled(string tvShowPageContent);

        bool AnyEpisodeHasAired(string pageContents);
    }
}
