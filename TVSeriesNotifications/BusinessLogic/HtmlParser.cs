using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVSeriesNotifications.CustomExceptions;

namespace TVSeriesNotifications.BusinessLogic
{
    public static class HtmlParser
    {
        public static IEnumerable<HtmlNode> SeasonNodes(string tvShowPageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(tvShowPageContent);

            var seasonsAndYearNode = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='seasons-and-year-nav']");
            if (seasonsAndYearNode is null)
                throw new ImdbHtmlChangedException("Cannot find \"class='seasons-and-year-nav'\" while searching for season section");

            var seasonNodes = seasonsAndYearNode.SelectNodes("div/a")
                ?.Where(IsSeasonLink)
                ?.OrderByDescending(o => int.Parse(o.InnerText.Trim()));

            if (seasonNodes is null)
                throw new ImdbHtmlChangedException("Cannot find season links while searching in season section");

            return seasonNodes;
        }

        public static bool ShowIsCancelled(string tvShowPageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(tvShowPageContent);

            var yearRangeNode = htmlDocument.DocumentNode.SelectSingleNode("//a[@title='See more release dates']");
            if (yearRangeNode is null)
                throw new ImdbHtmlChangedException("Cannot find \"title='See more release dates'\" in tv show page contents");

            var yearRangeStart = yearRangeNode.InnerText.IndexOf('(');
            var yearRangeEnd = yearRangeNode.InnerText.IndexOf(')');

            var yearRange = yearRangeNode.InnerText.Substring(yearRangeStart + 1, yearRangeEnd - yearRangeStart - 1);

            var years = yearRange.Split('–', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) // long dash (–) is used
                .Select(s => int.Parse(s))
                .ToArray();

            return years.Length == 2 && years[1] <= DateTime.Now.Year;
        }

        public static bool AnyEpisodeHasAired(string pageContents)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageContents);

            var airDatesText = htmlDocument.DocumentNode.SelectNodes("//div[@class='airdate']")?.Select(n => n.InnerText.Trim());
            if (airDatesText is null || !airDatesText.Any())
                throw new ImdbHtmlChangedException($"No air dates found");

            return SeasonAirDates(airDatesText).OrderBy(d => d).FirstOrDefault(d => d <= DateTime.Now.Date) != default;
        }

        private static IEnumerable<DateTime> SeasonAirDates(IEnumerable<string> airDatesText)
        {
            foreach (var dateText in airDatesText)
            {
                if (DateTime.TryParseExact(dateText, "d MMM. yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    yield return date;
            }
        }

        private static bool IsSeasonLink(HtmlNode node) =>
            node.Attributes.Any(a => a.Name == "href" && a.Value.Contains("season") && !a.Value.Contains("-1"))
            && !node.InnerText.ToLower().Equals("unknown")
            && !node.InnerText.ToLower().Contains("see all");
    }
}
