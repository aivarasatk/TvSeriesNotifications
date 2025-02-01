using System.Globalization;
using System.Text.Json.Serialization;
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

            var airDatesText = htmlDocument.DocumentNode.SelectNodes("//span[@class='sc-f2169d65-10 bYaARM']")?.Select(n => n.InnerText.Trim());
            var episodeIsRateable = htmlDocument.DocumentNode.SelectNodes("//div[@class='sc-e2dbc1a3-0 jeHPdh sc-663ab24a-3 dJmbUc']")?.Any(node => node.ChildNodes.Count != 0);
           
            if(episodeIsRateable is null)
            {
                throw new ImdbHtmlChangedException("Episode rating was not found");
            }

            if(airDatesText is null && episodeIsRateable.Value)
            {
                throw new ImdbHtmlChangedException("Episode air date was not found");
            }

            // Having no air date block and no episode rating means an unaired episode
            if (airDatesText is null && !episodeIsRateable.Value)
            {
                return false;
            }

            if (!airDatesText!.Any())
                return false;

            var nowDate = _dateTimeProvider.Now.Date;
            var defaultUpcomingReleaseDate = new DateTime(nowDate.Year, 1, 1); // when specified as "yyyy" in IMDB
            return SeasonAirDates(airDatesText!)
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