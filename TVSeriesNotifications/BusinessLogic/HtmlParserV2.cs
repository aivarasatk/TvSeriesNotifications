﻿using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using TVSeriesNotifications.CustomExceptions;
using TVSeriesNotifications.DateTimeProvider;

namespace TVSeriesNotifications.BusinessLogic
{
    public class HtmlParserV2 : HtmlParserBase, IHtmlParser
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public HtmlParserV2(IDateTimeProvider dateTimeProvider)
            : base(dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public bool AnyEpisodeHasAired(string pageContents) => base.AnyEpisodeHasAired(pageContents);

        public IEnumerable<int> Seasons(string tvShowPageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(tvShowPageContent);

            if (htmlDocument.DocumentNode.SelectSingleNode("//div[@class='ipc-button__text']") is not null)
                return new[] { 1 };

            var seasonListNode = htmlDocument.DocumentNode.SelectSingleNode("//select[@id='browse-episodes-season']");
            if (seasonListNode is null)
                throw new ImdbHtmlChangedException("Cannot find any season section in div[@class='ipc-button__text'] or select[@id='browse-episodes-season']");

            var seasonSelection = seasonListNode.SelectNodes("option");
            if (seasonSelection is null)
                throw new ImdbHtmlChangedException("Cannot find \"option\" while searching for season selection list");

            return seasonSelection
                .Where(node => int.TryParse(node.InnerText, out var dummy))
                .Select(node => int.Parse(node.InnerText));
        }

        public bool ShowIsCancelled(string tvShowPageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(tvShowPageContent);

            var yearRangeNode = htmlDocument.DocumentNode.SelectSingleNode("//span[@class='TitleBlockMetaData__ListItemText-sc-12ein40-2 jedhex']");
            if (yearRangeNode is null)
                throw new ImdbHtmlChangedException("Cannot find tv show year range in tv show page contents");

            var yearRange = yearRangeNode.InnerText;

            if (!yearRange.Contains('–')) // long dash (–) is used
                throw new ImdbHtmlChangedException($"Cannot find year range delimiter '–' in page contents {yearRange}'. e.g. \"2011–2019\"");

            var years = yearRange.Split('–', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.Parse(s))
                .ToArray();

            return years.Length == 2 && years[1] <= _dateTimeProvider.Now.Year;
        }
    }
}
