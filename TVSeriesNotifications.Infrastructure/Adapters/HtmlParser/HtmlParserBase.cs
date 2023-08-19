﻿using System.Globalization;
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

            var airDatesText = htmlDocument.DocumentNode.SelectNodes("//span[@class='sc-1318654d-10 jEHgCG']")?.Select(n => n.InnerText.Trim());
            if (airDatesText is null || !airDatesText.Any())
                return false;

            return SeasonAirDates(airDatesText).OrderBy(d => d).FirstOrDefault(d => d <= _dateTimeProvider.Now.Date) != default;
        }

        private static IEnumerable<DateTime> SeasonAirDates(IEnumerable<string> airDatesText)
        {
            foreach (var dateText in airDatesText)
            {
                if (DateTime.TryParseExact(dateText, new[] { "MMMM d, yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    yield return date;
            }
        }

        protected static bool IsSeasonLink(HtmlNode node) =>
            node.Attributes.Any(a => a.Name == "href" && a.Value.Contains("season") && !a.Value.Contains("-1"))
            && !node.InnerText.ToLower().Equals("unknown")
            && !node.InnerText.ToLower().Contains("see all");
    }
}