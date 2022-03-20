using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TVSeriesNotifications.Core.DateTimeProvider;
using TVSeriesNotifications.Domain.Models;
using TVSeriesNotifications.Domain.Ports.HtmlParser;
using TVSeriesNotifications.Domain.Ports.ImdbClient;
using TVSeriesNotifications.Domain.Ports.Repository;
using TVSeriesNotifications.Infrastructure.Adapters.HtmlParser.Exceptions;

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
                var firstUpcomingSeason = seasonNodes.LastOrDefault(s => SeasonHelper.IsUpcomingSeason(s, latestAiredSeason));

                if (!SeasonHelper.HasUpcomingSeason(firstUpcomingSeason) && _htmlParsingStrategies[tvShowId].ShowIsCancelled(tvShowPageContent))
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

            if (SeasonHelper.ShowIsOnGoing(tvShowSuggestion, _dateTimeProvider.Now))
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

        private async Task<(bool success, TvShow suggestion)> TryGetTvShowSuggestionAsync(string searchValue)
        {
            var suggestions = await _client.GetSuggestionsAsync(searchValue);

            var tvShow = suggestions.FirstOrDefault(s =>
                s.Title.ToLower().Equals(searchValue.ToLower())
                && s.Category is TVCategory.TVSeries or TVCategory.TVMiniSeries
                && s.YearStart > 2000);

            if (tvShow is null)
                return AsyncTryResponse<TvShow>(false);

            return AsyncTryResponse(success: true, tvShow);
        }

        public async Task<bool> UpcomingSeasonAired(string tvShowId, int firstUpcomingSeason)
            => firstUpcomingSeason is not 0 && await IsNewestAiredSeason(tvShowId, firstUpcomingSeason);

        private async Task<int> FindLatestAiredSeason(string tvShowId, ICollection<int> seasons)
        {
            foreach (var season in seasons)
            {
                if (await IsNewestAiredSeason(tvShowId, season))
                    return season;

                // When season for a new tv show is not out yet we'd still like to subscribe to it's notifications.
                if (SeasonHelper.TvShowToBeAired(season, seasons.Count))
                    return 0; // Design flaw. We need to have a numeric value for a latest aired season
            }

            throw new ImdbHtmlChangedException("No latest aired season found");
        }

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

        private (bool success, T value) AsyncTryResponse<T>(bool success, T value = default) 
            => (success, value);

    }
}
