using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TVSeriesNotifications.Api;
using TVSeriesNotifications.Common;
using TVSeriesNotifications.CustomExceptions;
using TVSeriesNotifications.DateTimeProvider;
using TVSeriesNotifications.DTO;
using TVSeriesNotifications.JsonModels;
using TVSeriesNotifications.Persistance;

namespace TVSeriesNotifications.BusinessLogic
{
    public class SeasonChecker : ISeasonChecker
    {
        private Dictionary<string, IHtmlParser> _htmlParsingStrategies = new();
        private object _parsingStrategyLock = new();

        private readonly IImdbClient _client;
        private readonly IHtmlParserStrategyFactory _htmlParserStrategyFactory;
        private readonly IPersistantCache<string> _cacheTvShowIds;
        private readonly IPersistantCache<string> _cacheIgnoredTvShows;
        private readonly IPersistantCache<int> _cacheLatestAiredSeasons;
        private readonly IDateTimeProvider _dateTimeProvider;

        public SeasonChecker(
            IImdbClient client,
            IHtmlParserStrategyFactory htmlParserStrategyFactory,
            IPersistantCache<string> cacheTvShowIds,
            IPersistantCache<string> cacheIgnoredTvShows,
            IPersistantCache<int> cacheLatestAiredSeasons,
            IDateTimeProvider dateTimeProvider)
        {
            _client = client;
            _htmlParserStrategyFactory = htmlParserStrategyFactory;
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
            var (success, tvShowId) = await TryGetTvShowId(tvShow);
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

            var parser = _htmlParserStrategyFactory.ResolveParsingStrategy(tvShowPageContent);

            lock (_parsingStrategyLock)
            {
                _htmlParsingStrategies.TryAdd(tvShowId, parser);
            }

            var seasonNodes = _htmlParsingStrategies[tvShowId].Seasons(tvShowPageContent)
                .OrderByDescending(season => season)
                .ToArray();

            if (_cacheLatestAiredSeasons.TryGet(tvShow, out int latestAiredSeason))
            {
                var firstUpcomingSeason = seasonNodes.Where(s => IsUpcomingSeason(s, latestAiredSeason)).LastOrDefault();

                if (!HasUpcomingSeason(firstUpcomingSeason) && _htmlParsingStrategies[tvShowId].ShowIsCancelled(tvShowPageContent))
                {
                    MarkShowAsCancelled(tvShow);
                    return AsyncTryResponse<NewSeason>(false, null);
                }

                if (await UpcomingSeasonAired(tvShowId, firstUpcomingSeason))
                {
                    _cacheLatestAiredSeasons.Update(tvShow, firstUpcomingSeason);

                    return AsyncTryResponse(true, new NewSeason(tvShow, firstUpcomingSeason));
                }
            }
            else
            {
                var airedSeason = await FindLatestAiredSeason(tvShowId, seasonNodes);
                _cacheLatestAiredSeasons.Add(tvShow, airedSeason);
            }

            return AsyncTryResponse<NewSeason>(false, null);
        }

        private static bool HasUpcomingSeason(int firstUpcomingSeason) => firstUpcomingSeason != default;

        private async Task<bool> UpcomingSeasonAired(string tvShowId, int firstUpcomingSeason)
            => firstUpcomingSeason is not 0 && await IsNewestAiredSeason(tvShowId, firstUpcomingSeason);

        private void MarkShowAsCancelled(string searchValue)
        {
            _cacheIgnoredTvShows.Add(searchValue, string.Empty);
            _cacheTvShowIds.Remove(searchValue);
            _cacheLatestAiredSeasons.Remove(searchValue);
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
            // Ongoing examples 2018-, 2018-2100(ends in current year + n years)
            var yearRangeSplit = tvShowSuggestion.YearRange
                .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(y => int.Parse(y))
                .ToArray();

            return tvShowSuggestion.Category != TVCategory.TVMiniSeries
                && (tvShowSuggestion.YearRange.Last() == '-' || (yearRangeSplit.Length == 2 && yearRangeSplit[1] >= _dateTimeProvider.Now.Year));
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

        private static bool IsUpcomingSeason(int currentSeason, int latestAiredSeason) // there can be more than one confirmed seasons
            => currentSeason > latestAiredSeason;

        private async Task<int> FindLatestAiredSeason(string tvShowId, ICollection<int> seasons)
        {
            foreach (var season in seasons)
            {
                if (await IsNewestAiredSeason(tvShowId, season))
                    return season;

                // When season for a new tv show is not out yet we'd still like to subscribe to it's notifications.
                if (TvShowToBeAired(season, seasons.Count))
                    return 0; // Design flaw. We need to have a numeric value for a latest aired season
            }

            throw new ImdbHtmlChangedException("No latest aired season found");
        }

        private static bool TvShowToBeAired(int season, int seasonCount) => season is 1 && seasonCount is 1;

        private async Task<bool> IsNewestAiredSeason(string tvShowId, int season)
        {
            try
            {
                var content = await _client.GetSeasonPageContentsAsync(tvShowId, season);
                return _htmlParsingStrategies[tvShowId].AnyEpisodeHasAired(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during air date search for {tvShowId} season {season}", ex);
            }
        }
    }
}
