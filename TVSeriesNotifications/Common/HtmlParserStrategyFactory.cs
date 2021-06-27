using System;
using HtmlAgilityPack;
using TVSeriesNotifications.BusinessLogic;

namespace TVSeriesNotifications.Common
{
    public class HtmlParserStrategyFactory : IHtmlParserStrategyFactory
    {
        public IHtmlParser ResolveParsingStrategy(string tvShowPageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(tvShowPageContent);

            if (IsV1Strategy(htmlDocument))
                return new HtmlParserV1(new DateTimeProvider.DateTimeProvider());

            if (IsV2Strategy(htmlDocument))
                return new HtmlParserV2(new DateTimeProvider.DateTimeProvider());

            throw new ArgumentOutOfRangeException("Could resolve HTML parser strategy");
        }

        private static bool IsV1Strategy(HtmlDocument htmlDocument)
        {
            var node = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='seasons-and-year-nav']");
            return node is not null;
        }

        private static bool IsV2Strategy(HtmlDocument htmlDocument)
        {
            var node = htmlDocument.DocumentNode.SelectSingleNode("//select[@id='browse-episodes-season']");

            if (node is not null)
                return true;

            node = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='ipc-button__text']");

            if (node is not null)
                return true;

            return false;
        }
    }
}
