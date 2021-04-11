using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using TVSeriesNotifications.Api;
using TVSeriesNotifications.CustomExceptions;
using TVSeriesNotifications.DateTimeProvider;
using TVSeriesNotifications.DTO;
using TVSeriesNotifications.JsonModels;
using TVSeriesNotifications.Persistance;

namespace TVSeriesNotifications.BusinessLogic
{
    public class SeasonChecker : ISeasonChecker
    {
        private readonly IImdbClient _client;
        private readonly IHtmlParser _htmlParser;
        private readonly IPersistantCache<string> _cacheTvShowIds;
        private readonly IPersistantCache<string> _cacheIgnoredTvShows;
        private readonly IPersistantCache<int> _cacheLatestAiredSeasons;
        private readonly IDateTimeProvider _dateTimeProvider;

        public SeasonChecker(
            IImdbClient client,
            IHtmlParser htmlParser,
            IPersistantCache<string> cacheTvShowIds,
            IPersistantCache<string> cacheIgnoredTvShows,
            IPersistantCache<int> cacheLatestAiredSeasons,
            IDateTimeProvider dateTimeProvider)
        {
            _client = client;
            _htmlParser = htmlParser;
            _cacheTvShowIds = cacheTvShowIds;
            _cacheIgnoredTvShows = cacheIgnoredTvShows;
            _cacheLatestAiredSeasons = cacheLatestAiredSeasons;
            _dateTimeProvider = dateTimeProvider;
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
            return await NewSeasonStatus(tvShow, tvShowId);
        }

        private async Task<(bool newSeasonAired, NewSeason seasonInfo)> NewSeasonStatus(string tvShow, string tvShowId)
        {
            var tvShowPageContent = await _client.GetPageContentsAsync(tvShowId);

            var seasonNodes = _htmlParser.SeasonNodes(tvShowPageContent)
                .OrderByDescending(n => int.Parse(n.InnerText))
                .ToArray();

            if (_cacheLatestAiredSeasons.TryGet(tvShow, out int latestAiredSeason))
            {
                var firstUpcomingSeason = seasonNodes.Where(s => IsUpcomingSeason(s, latestAiredSeason)).LastOrDefault();

                if (firstUpcomingSeason is null && _htmlParser.ShowIsCancelled(tvShowPageContent))
                {
                    MarkShowAsCancelled(tvShow);
                    return AsyncTryResponse<NewSeason>(false, null);
                }

                if (await UpcomingSeasonAired(firstUpcomingSeason))
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

        private async Task<bool> UpcomingSeasonAired(SeasonNode firstUpcomingSeason)
            => firstUpcomingSeason is not null && await IsNewestAiredSeason(firstUpcomingSeason.Attributes.Single(a => a.Name == "href").Value);

        private void MarkShowAsCancelled(string searchValue)
        {
            _cacheIgnoredTvShows.Add(searchValue, string.Empty);
            _cacheTvShowIds.Remove(searchValue);
            _cacheLatestAiredSeasons.Remove(searchValue);
        }

        private async Task SetLatestAiredSeason(string searchValue, IEnumerable<SeasonNode> seasonNodes)
        {
            try
            {
                var latestAiredSeason = await FindLatestAiredSeason(seasonNodes);
                _cacheLatestAiredSeasons.Add(searchValue, latestAiredSeason);
            }
            catch (ImdbHtmlChangedException iex)
            {
                throw new ImdbHtmlChangedException($"TvShow: {searchValue}", iex);
            }
        }

        private async Task<(bool success, string tvShowId)> TryGetTvShowId(string searchValue)
        {
            if (_cacheIgnoredTvShows.Exists(searchValue))
                return AsyncTryResponse<string>(success: false);

            if (_cacheTvShowIds.TryGet(searchValue, out var tvShowId))
                return AsyncTryResponse(success: true, tvShowId);

            var (success, tvShowSuggestion) = await TryGetTvShowSuggestionAsync(searchValue);
            if (!success)
            {
                _cacheIgnoredTvShows.Add(searchValue, string.Empty);
                return AsyncTryResponse<string>(success: false);
            }

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

        private (bool success, T value) AsyncTryResponse<T>(bool success, T value = default)
            => (success, value);

        private bool ShowIsOnGoing(Suggestion tvShowSuggestion)
        {
            // Ended examples 2019, 2015-2019
            // Ongoing examples 2018-, 2018-2021(ends in current year + n years)
            var yearRangeSplit = tvShowSuggestion.YearRange
                .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(y => int.Parse(y))
                .ToArray();

            return tvShowSuggestion.Category != TVCategory.TVMiniSeries
                && (tvShowSuggestion.YearRange.Last() == '-' || (yearRangeSplit.Length == 2 && yearRangeSplit[1] > _dateTimeProvider.Now.Year));
        }

        private async Task<(bool success, Suggestion suggestion)> TryGetTvShowSuggestionAsync(string searchValue)
        {
            var suggestions = await _client.GetSuggestionsAsync(searchValue);

            var tvShow = suggestions?.Suggestions?.FirstOrDefault(s =>
                s.Title.ToLower().Equals(searchValue.ToLower())
                && s.Category == TVCategory.TVSeries
                && s.YearStart > 2000);

            if (tvShow is null)
                return AsyncTryResponse<Suggestion>(false);

            return AsyncTryResponse(success: true, tvShow);
        }

        private bool IsUpcomingSeason(SeasonNode arg, int latestAiredSeason) // there can be more than one confirmed seasons
            => int.Parse(arg.InnerText.Trim()) > latestAiredSeason;

        private async Task<int> FindLatestAiredSeason(IEnumerable<SeasonNode> seasonLinks)
        {
            var seasons = seasonLinks.Select(l => (season: int.Parse(l.InnerText), link: l.Attributes.Single(a => a.Name == "href").Value))
                .OrderByDescending(o => o.season)
                .ToArray();

            foreach (var (season, link) in seasons)
            {
                if (await IsNewestAiredSeason(link))
                    return season;

                // When season for a new tv show is not out yet we'd still like to subscribe to it's notifications.
                if (TvShowToBeAired(season, seasons.Length))
                    return 0; // Design flaw. We need to have a numeric value for a latest aired season
            }

            throw new ImdbHtmlChangedException("No latest aired season found");
        }

        private bool TvShowToBeAired(int season, int seasonCount) => season is 1 && seasonCount is 1;

        private async Task<bool> IsNewestAiredSeason(string link)
        {
            var content = await _client.GetSeasonPageContentsAsync(link);

            try
            {
                return _htmlParser.AnyEpisodeHasAired(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during air date search for link {link}", ex);
            }
        }
    }
}
