using System.Globalization;
using HtmlAgilityPack;
using TVSeriesNotifications.Core.DateTimeProvider;
using TVSeriesNotifications.Infrastructure.Adapters.HtmlParser.Exceptions;

namespace TVSeriesNotifications.Infrastructure.Adapters.HtmlParser
{
    public class HtmlParserBase
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public HtmlParserBase(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        protected bool AnyEpisodeHasAired(string pageContents)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageContents);

            var airDatesText = htmlDocument.DocumentNode.SelectNodes("//span[@class='sc-ccd6e31b-10 fVspdm']")?.Select(n => n.InnerText.Trim());

            if (airDatesText is null)
                throw new ImdbHtmlChangedException("Episode air date span[@class='sc-ccd6e31b-10 fVspdm'] was not found");

            if (!airDatesText.Any())
                return false;

            var nowDate = _dateTimeProvider.Now.Date;
            var defaultUpcomingReleaseDate = new DateTime(nowDate.Year, 1, 1); // when specified as "yyyy" in IMDB
            return SeasonAirDates(airDatesText)
                .OrderBy(d => d)
                .FirstOrDefault(d => d <= nowDate && d != defaultUpcomingReleaseDate) != default;
        }

        private static IEnumerable<DateTime> SeasonAirDates(IEnumerable<string> airDatesText)
        {
            foreach (var dateText in airDatesText)
            {
                if (DateTime.TryParseExact(dateText, new[] { "ddd, MMM d, yyyy", "MMM yyyy", "yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    yield return date;
            }
        }

        protected static bool IsSeasonLink(HtmlNode node) =>
            node.Attributes.Any(a => a.Name == "href" && a.Value.Contains("season") && !a.Value.Contains("-1"))
            && !node.InnerText.ToLower().Equals("unknown")
            && !node.InnerText.ToLower().Contains("see all");
    }
}