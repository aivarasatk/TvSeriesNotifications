using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using TVSeriesNotifications.Api;
using TVSeriesNotifications.CustomExceptions;
using TVSeriesNotifications.DTO;
using TVSeriesNotifications.JsonModels;
using TVSeriesNotifications.Persistance;

namespace TVSeriesNotifications.BusinessLogic
{
    public class SeasonChecker : ISeasonChecker
    {
        private readonly IImdbClient _client;
        private readonly IPersistantCache<string> _cacheTvShowIds;
        private readonly IPersistantCache<string> _cacheIgnoredTvShows;
        private readonly IPersistantCache<int> _cacheLatestAiredSeasons;

        public SeasonChecker(
            IImdbClient client,
            IPersistantCache<string> cacheTvShowIds,
            IPersistantCache<string> cacheIgnoredTvShows,
            IPersistantCache<int> cacheLatestAiredSeasons)
        {
            _client = client;
            _cacheTvShowIds = cacheTvShowIds;
            _cacheIgnoredTvShows = cacheIgnoredTvShows;
            _cacheLatestAiredSeasons = cacheLatestAiredSeasons;
        }

        /// <summary>
        /// Checks if passed tv show has a new aired season. If no prior information about the tv show exists latest aired season is saved.
        /// </summary>
        /// <param name="tvShow">TV show name.</param>
        /// <returns>Value tuple that indicates if new season is aired and its value. If no prior information about the show exists it is treated as no new season aired.</returns>
        /// <exception cref="ImdbHtmlChangedException">Throws when HTML is changed and excpected nodes are not found.</exception>
        /// <exception cref="Exception">All other general expections.</exception>
        public async Task<(bool newSeasonAired, NewSeason seasonInfo)> TryCheckForNewSeasonAsync(string tvShow)
        {
            var (success, tvShowId) = await TryGetTvShowId(tvShow).ConfigureAwait(false);
            if (!success)
                return AsyncTryResponse<NewSeason>(false, null);
#if DEBUG
            Console.WriteLine(tvShow);
#endif
            var tvShowPageContent = await _client.GetPageContentsAsync(tvShowId);

            var seasonNodes = SeasonNodes(tvShowPageContent).ToArray();

            if (_cacheLatestAiredSeasons.TryGet(tvShow, out int latestAiredSeason))
            {
                var firstUpcomingSeason = seasonNodes.Where(s => IsUpcomingSeason(s, latestAiredSeason)).LastOrDefault();

                if (firstUpcomingSeason is null && ShowIsCancelled(tvShowPageContent))
                {
                    MarkShowAsCancelled(tvShow);
                }
                else if (firstUpcomingSeason is not null && await UpcomingSeasonAired(firstUpcomingSeason))
                {
                    var season = int.Parse(firstUpcomingSeason.InnerText);
                    _cacheLatestAiredSeasons.Update(tvShow, season);

                    return AsyncTryResponse(true, new NewSeason(tvShow, season));
                }
            }
            else
            {
                await SetLatestAiredSeason(tvShow, seasonNodes).ConfigureAwait(false);
            }

            return AsyncTryResponse<NewSeason>(false, null);
        }

        private async Task<bool> UpcomingSeasonAired(HtmlNode firstUpcomingSeason)
            => await IsNewestAiredSeason(firstUpcomingSeason.Attributes.Single(a => a.Name == "href").Value);

        private void MarkShowAsCancelled(string searchValue)
        {
            _cacheIgnoredTvShows.Add(searchValue, string.Empty);
            _cacheTvShowIds.Remove(searchValue);
            _cacheLatestAiredSeasons.Remove(searchValue);
        }

        private bool ShowIsCancelled(string tvShowPageContent)
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

        private async Task SetLatestAiredSeason(string searchValue, IEnumerable<HtmlNode> seasonNodes)
        {
            var latestAiredSeason = await FindLatestAiredSeason(seasonNodes);
            _cacheLatestAiredSeasons.Add(searchValue, latestAiredSeason);
        }

        private async Task<(bool success, string tvShowId)> TryGetTvShowId(string searchValue)
        {
            if (_cacheIgnoredTvShows.Exists(searchValue))
                return AsyncTryResponse<string>(success: false);

            if (_cacheTvShowIds.TryGet(searchValue, out var tvShowId))
                return AsyncTryResponse(success: true, tvShowId);

            var (success, tvShowSuggestion) = await TryGetTvShowSuggestionAsync(searchValue);
            if (!success)
                return AsyncTryResponse<string>(success: false);

            if (ShowIsOnGoing(tvShowSuggestion))
            {
                _cacheTvShowIds.Add(searchValue, tvShowSuggestion.Id);
                return AsyncTryResponse(success: true, tvShowSuggestion.Id);
            }
            else
            {
                _cacheIgnoredTvShows.Add(searchValue, string.Empty);
                return AsyncTryResponse<string>(success: false);
            }
        }

        private (bool success, T tvShowId) AsyncTryResponse<T>(bool success, T id = default)
            => (success, id);

        private bool ShowIsOnGoing(Suggestion tvShowSuggestion)
        {
            // Ended examples 2019, 2015-2019
            // Ongoing examples 2018-, 2018-2021(ends in current year + n years)
            var yearRangeSplit = tvShowSuggestion.YearRange
                .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(y => int.Parse(y))
                .ToArray();

            return tvShowSuggestion.Category != TVCategory.TVMiniSeries
                && (tvShowSuggestion.YearRange.Last() == '-' || (yearRangeSplit.Length == 2 && yearRangeSplit[1] > DateTime.Now.Year));
        }

        private IEnumerable<HtmlNode> SeasonNodes(string tvShowPageContent)
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

        private async Task<(bool success, Suggestion suggestion)> TryGetTvShowSuggestionAsync(string searchValue)
        {
            var urlReadySearchValue = $"{searchValue.Replace(' ', '_')}.json";
            var index = searchValue.Substring(0, 1).ToLower();

            var suggestions = await _client.GetSuggestionsAsync(searchValue);

            var tvShow = suggestions?.Suggestions?.FirstOrDefault(s =>
                s.Title.ToLower().Equals(searchValue.ToLower())
                && s.Category == TVCategory.TVSeries
                && s.YearStart > 2000);

            if (tvShow is null)
            {
                _cacheIgnoredTvShows.Add(searchValue, string.Empty);
                return AsyncTryResponse<Suggestion>(false);
            }

            return AsyncTryResponse(success: true, tvShow);
        }

        private bool IsUpcomingSeason(HtmlNode arg, int latestAiredSeason)// there can be more than one confirmed seasons
            => int.Parse(arg.InnerText.Trim()) > latestAiredSeason;


        private async Task<int> FindLatestAiredSeason(IEnumerable<HtmlNode> seasonLinks)
        {
            foreach (var linkNode in seasonLinks)
            {
                var link = linkNode.Attributes.Single(a => a.Name == "href").Value;
                if (await IsNewestAiredSeason(link))
                    return int.Parse(linkNode.InnerText);
            }

            throw new ImdbHtmlChangedException("No latest aired season found");
        }

        private async Task<bool> IsNewestAiredSeason(string link)
        {
            var content = await _client.GetSeasonPageContentsAsync(link);

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);

            var airDatesText = htmlDocument.DocumentNode.SelectNodes("//div[@class='airdate']")?.Select(n => n.InnerText.Trim());
            if (airDatesText is null || !airDatesText.Any())
                throw new ImdbHtmlChangedException($"No air dates found for season {link}");

            return SeasonAirDates(airDatesText).OrderBy(d => d).FirstOrDefault(d => d <= DateTime.Now.Date) != default;
        }

        private IEnumerable<DateTime> SeasonAirDates(IEnumerable<string> airDatesText)
        {
            foreach (var dateText in airDatesText)
            {
                if (DateTime.TryParseExact(dateText, "d MMM. yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    yield return date;
            }
        }

        private bool IsSeasonLink(HtmlNode node) =>
            node.Attributes.Any(a => a.Name == "href" && a.Value.Contains("season") && !a.Value.Contains("-1"))
            && !node.InnerText.ToLower().Equals("unknown")
            && !node.InnerText.ToLower().Contains("see all");
    }
}
