using HtmlAgilityPack;
using TVSeriesNotifications.Core.DateTimeProvider;
using TVSeriesNotifications.Domain.Ports.HtmlParser;
using TVSeriesNotifications.Infrastructure.Adapters.HtmlParser.Exceptions;

namespace TVSeriesNotifications.Infrastructure.Adapters.HtmlParser
{
    public class HtmlParserV1 : HtmlParserBase, IHtmlParser
    {
        public HtmlParserV1(IDateTimeProvider dateTimeProvider)
            : base(dateTimeProvider)
        {
        }

        public bool AnyEpisodeHasAired(string pageContents) => base.AnyEpisodeHasAired(pageContents);

        public IEnumerable<int> Seasons(string tvShowPageContent)
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

            return seasonNodes.Select(node => int.Parse(node.InnerText));
        }

        public bool ShowIsCancelled(string tvShowPageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(tvShowPageContent);

            var yearRangeNode = htmlDocument.DocumentNode.SelectSingleNode("//a[@title='See more release dates']");
            if (yearRangeNode is null)
                throw new ImdbHtmlChangedException("Cannot find \"title='See more release dates'\" in tv show page contents");

            var yearRangeStart = yearRangeNode.InnerText.IndexOf('(');
            var yearRangeEnd = yearRangeNode.InnerText.IndexOf(')');

            if (yearRangeStart == -1 || yearRangeEnd == -1)
                throw new ImdbHtmlChangedException($"Cannot find year range in page contents in '{yearRangeNode.InnerText}'. e.g. \"TV Series (2011–2019)\"");

            var yearRange = yearRangeNode.InnerText.Substring(yearRangeStart + 1, yearRangeEnd - yearRangeStart - 1);

            if (!yearRange.Contains('–'))
                throw new ImdbHtmlChangedException($"Cannot find year range delimiter '–' in page contents {yearRangeNode.InnerText}'. e.g. \"TV Series (2011–2019)\"");

            var years = yearRange.Split('–', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) // long dash (–) is used
                .Select(s => int.Parse(s))
                .ToArray();

            return years.Length == 2; // year range indicates cancelation
        }
    }
}
