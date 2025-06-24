using System.Globalization;
using HtmlAgilityPack;
using TVSeriesNotifications.Core.DateTimeProvider;
using TVSeriesNotifications.Infrastructure.Adapters.HtmlParser.Exceptions;

namespace TVSeriesNotifications.Infrastructure.Adapters.HtmlParser
{
    public class HtmlParserBase
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private HtmlElement _airDateElement;

        public HtmlParserBase(IDateTimeProvider dateTimeProvider, HtmlElement airDateElement)
        {
            _dateTimeProvider = dateTimeProvider;
            _airDateElement = airDateElement;
        }

        protected bool AnyEpisodeHasAired(string pageContents)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageContents);

            var airDatesText = htmlDocument.DocumentNode.SelectNodes($"//{_airDateElement.Tag}[@{_airDateElement.Attribute}='{_airDateElement.Value}']")?.Select(n => n.InnerText.Trim());

            // Having no air date block means an unaired episode
            if (airDatesText is null)
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
                if (TryParseAirDate(dateText, out var date))
                    yield return date;
            }
        }

        private static bool TryParseAirDate(string airDateText, out DateTime date)
        {
            return DateTime.TryParseExact(airDateText, new[] { "ddd, MMM d, yyyy", "MMM yyyy", "yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        public static HtmlElement TryResolveHtmlElement(string content, string keyword)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);

            var keywordIndex = htmlDocument.ParsedText.IndexOf(keyword);
            if (keywordIndex is -1)
            {
                throw new Exception($"keyword not found in content: [{keyword}]");
            }

            int elementStart, elementEnd;

            elementStart = htmlDocument.ParsedText.LastIndexOf('<', keywordIndex);
            if (elementStart is -1)
            {
                throw new Exception($"HTML element start not found in content: [{keyword}], keyword start: {keywordIndex}");
            }

            elementEnd = htmlDocument.ParsedText.IndexOf('>', elementStart);
            if (elementEnd is -1)  
            {
                throw new Exception($"HTML element end not found in content: [{keyword}], keyword start: {keywordIndex}, element start: {elementStart}");
            }

            var elementInnerPart = htmlDocument.ParsedText.Substring(elementStart+1, elementEnd - elementStart);

            var parts = elementInnerPart.Split(" ");
            if (parts.Length is 0)
            {
                throw new Exception($"keyword not found in content: [{keyword}]");
            }
            if (!validHtmlTags.Contains(parts[0]))
            {
                throw new Exception($"element around keyword is not a valid HTML tag, substring: [{elementInnerPart}]");
            }

            if (!parts[1].StartsWith("class"))
            {
                throw new Exception($"attribute was not class: {parts[1]}");
            }

            int classValueStart, classValueEnd;
            classValueStart = htmlDocument.ParsedText.IndexOf("\"", elementStart);
            classValueEnd = htmlDocument.ParsedText.IndexOf("\"", classValueStart+1);
            var classValue = htmlDocument.ParsedText.Substring(classValueStart+1, classValueEnd-classValueStart - 1);

            return new HtmlElement(parts[0], "class", classValue);
        }

        static HashSet<string> validHtmlTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "div", "span", "p", "a", "ul", "li", "table", "tr", "td", "img", "input", "form",
            "button", "h1", "h2", "h3", "h4", "h5", "h6", "strong", "em", "br", "hr", "nav"
        };

    }

    public record HtmlElement(string Tag, string Attribute, string Value);
}