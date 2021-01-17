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
            return await NewSeasonStatus(tvShow, tvShowId);
        }

        private async Task<(bool newSeasonAired, NewSeason seasonInfo)> NewSeasonStatus(string tvShow, string tvShowId)
        {
            var tvShowPageContent = await _client.GetPageContentsAsync(tvShowId);

            var seasonNodes = HtmlParser.SeasonNodes(tvShowPageContent).ToArray();

            if (_cacheLatestAiredSeasons.TryGet(tvShow, out int latestAiredSeason))
            {
                var firstUpcomingSeason = seasonNodes.Where(s => IsUpcomingSeason(s, latestAiredSeason)).LastOrDefault();

                if (firstUpcomingSeason is null && HtmlParser.ShowIsCancelled(tvShowPageContent))
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

        private async Task<bool> UpcomingSeasonAired(HtmlNode firstUpcomingSeason)
            => firstUpcomingSeason is not null && await IsNewestAiredSeason(firstUpcomingSeason.Attributes.Single(a => a.Name == "href").Value);

        private void MarkShowAsCancelled(string searchValue)
        {
            _cacheIgnoredTvShows.Add(searchValue, string.Empty);
            _cacheTvShowIds.Remove(searchValue);
            _cacheLatestAiredSeasons.Remove(searchValue);
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
                && (tvShowSuggestion.YearRange.Last() == '-' || (yearRangeSplit.Length == 2 && yearRangeSplit[1] > DateTime.Now.Year));
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

            try
            {
                return HtmlParser.AnyEpisodeHasAired(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during air date search for link {link}", ex);
            }
        }
    }
}
