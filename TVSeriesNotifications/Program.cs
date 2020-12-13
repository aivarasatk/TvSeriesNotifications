using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using SafeParallel;
using TVSeriesNotifications.CustomExceptions;
using TVSeriesNotifications.JsonModels;
using TVSeriesNotifications.Notifications;
using TVSeriesNotifications.Persistance;

namespace TVSeriesNotifications
{
    class Program
    {
        private const string BaseSuggestionUrl = "https://v2.sg.media-imdb.com/suggestion";
        private const string BaseImdbUrl = "https://www.imdb.com";
        private const string BaseTitleSearch = "https://www.imdb.com/title";

        private static readonly int _coreCount;

        private static readonly HttpClient _httpClient;

        private static readonly IPersistantCache _cacheTvShowIds;
        private static readonly IPersistantCache _cacheIgnoredTvShows;
        private static readonly IPersistantCache _cacheLatestAiredSeasons;

        private static readonly INotificationService _notificationService;
        private static readonly ITvShowRepository _tvShowRepository;


        static Program()
        {
            _coreCount = Environment.ProcessorCount * 2;//yields 40% better performace than ProcessorCount. And 8.3x faster than single core
            _httpClient = new HttpClient();

            _cacheTvShowIds = new PersistantCache("Cache/TvShowIds");
            _cacheIgnoredTvShows = new PersistantCache("Cache/IgnoredTvShows");
            _cacheLatestAiredSeasons = new PersistantCache("Cache/LatestAiredSeasons");

            _notificationService = new FileNotificationService();
            _tvShowRepository = new FileTvShowRepository();
        }

        public static async Task Main()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var tvShows = await _tvShowRepository.RetrieveTvShows();
                await CheckForNewSeasons(tvShows);
            }
            catch (Exception ex) when (ex is not ImdbHtmlChangedException)
            {
                await _notificationService.NotifyAboutErrors($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }

            Console.WriteLine($"Elapsed: {stopwatch.Elapsed.TotalSeconds}");
            Console.ReadKey();
        }

        private static async Task CheckForNewSeasons(IEnumerable<string> tvShows)
        {
            var shows = tvShows.Except(_cacheIgnoredTvShows.Keys()).ToArray();
            await shows.SafeParallelAsync(async tvShow => await CheckForNewSeason(tvShow)).ConfigureAwait(false);
        }

        private static async Task CheckForNewSeason(string tvshow)
        {
            try
            {
                var (success, tvShowId) = await TryGetTvShowId(tvshow);
                if (!success)
                    return;

                Console.WriteLine(tvshow);

                var tvShowPageContent = await GetRequestAsync(Path.Combine(BaseTitleSearch, tvShowId) + "/");

                var seasonNodes = SeasonNodes(tvShowPageContent);

                if (_cacheLatestAiredSeasons.TryGet(tvshow, out int latestAiredSeason))
                {
                    var firstUpcomingSeason = seasonNodes.Where(s => IsUpcomingSeason(s, latestAiredSeason)).LastOrDefault();

                    if (firstUpcomingSeason is null && ShowIsCancelled(tvShowPageContent))
                        MarkShowAsCancelled(tvshow);
                    else if (firstUpcomingSeason is not null && await UpcomingSeasonAired(firstUpcomingSeason))
                        await NotifyNewSeasonAired(tvshow, int.Parse(firstUpcomingSeason.InnerText));
                }
                else
                {
                    await SetLatestAiredSeason(tvshow, seasonNodes);
                }
            }
            catch (ImdbHtmlChangedException iex)
            {
                await _notificationService.NotifyAboutErrors($"{DateTime.Now}: HTML changed: {iex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                await _notificationService.NotifyAboutErrors($"{DateTime.Now}: Unexpected exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                throw;
            }
        }

        private static async Task NotifyNewSeasonAired(string searchValue, int season)
        {
            _cacheLatestAiredSeasons.Update(searchValue, season);
            await _notificationService.NotifyAboutNewSeason(searchValue, season);
        }

        private static async Task<bool> UpcomingSeasonAired(HtmlNode firstUpcomingSeason)
            => await IsNewestAiredSeason(firstUpcomingSeason.Attributes.Single(a => a.Name == "href").Value);

        private static void MarkShowAsCancelled(string searchValue)
        {
            _cacheIgnoredTvShows.Add(searchValue, string.Empty);
            _cacheTvShowIds.Remove(searchValue);
            _cacheLatestAiredSeasons.Remove(searchValue);
        }

        private static bool ShowIsCancelled(string tvShowPageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(tvShowPageContent);

            var yearRangeNode = htmlDocument.DocumentNode.SelectSingleNode("//a[@title='See more release dates']");
            if (yearRangeNode is null)
                throw new ImdbHtmlChangedException("Cannot find \"title='See more release dates'\" in tv show page contents");

            var yearRangeStart = yearRangeNode.InnerText.IndexOf('(');
            var yearRangeEnd = yearRangeNode.InnerText.IndexOf(')');

            var yearRange = yearRangeNode.InnerText.Substring(yearRangeStart + 1, yearRangeEnd - yearRangeStart - 1);

            var years = yearRange.Split('–', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)//long dash (–) is used
                .Select(s => int.Parse(s))
                .ToArray();

            return years.Length == 2 && years[1] <= DateTime.Now.Year;
        }

        private static async Task SetLatestAiredSeason(string searchValue, IEnumerable<HtmlNode> seasonNodes)
        {
            var latestAiredSeason = await FindLatestAiredSeason(seasonNodes);
            _cacheLatestAiredSeasons.Add(searchValue, latestAiredSeason);
        }

        private static async Task<(bool success, string tvShowId)> TryGetTvShowId(string searchValue)
        {
            if (_cacheIgnoredTvShows.Exists(searchValue))
                return AsyncTryResponse<string>(success: false);

            if (_cacheTvShowIds.TryGet(searchValue, out string tvShowId))
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

        private static (bool success, T tvShowId) AsyncTryResponse<T>(bool success, T id = default)
            => (success, id);

        private static bool ShowIsOnGoing(Suggestion tvShowSuggestion)
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

        private static IEnumerable<HtmlNode> SeasonNodes(string tvShowPageContent)
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

        private static async Task<(bool success, Suggestion suggestion)> TryGetTvShowSuggestionAsync(string searchValue)
        {
            var urlReadySearchValue = $"{searchValue.Replace(' ', '_')}.json";
            var index = searchValue.Substring(0, 1).ToLower();

            var suggestions = await GetRequestAsync<ImdbSuggestion>(Path.Combine(BaseSuggestionUrl, index, urlReadySearchValue));

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

        private static bool IsUpcomingSeason(HtmlNode arg, int latestAiredSeason)// there can be more than one confirmed seasons
            => int.Parse(arg.InnerText.Trim()) > latestAiredSeason;


        private static async Task<int> FindLatestAiredSeason(IEnumerable<HtmlNode> seasonLinks)
        {
            foreach (var linkNode in seasonLinks)
            {
                var link = linkNode.Attributes.Single(a => a.Name == "href").Value;
                if (await IsNewestAiredSeason(link))
                    return int.Parse(linkNode.InnerText);
            }

            throw new ImdbHtmlChangedException("No latest aired season found");
        }

        private static async Task<bool> IsNewestAiredSeason(string link)
        {
            var content = await GetRequestAsync($"{BaseImdbUrl}{link}");

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);

            var airDatesText = htmlDocument.DocumentNode.SelectNodes("//div[@class='airdate']")?.Select(n => n.InnerText.Trim());
            if (airDatesText is null || !airDatesText.Any())
                throw new ImdbHtmlChangedException($"No air dates found for season {link}");

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

        private static async Task<string> GetRequestAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task<T> GetRequestAsync<T>(string url)
        {
            var content = await GetRequestAsync(url);
            return JsonConvert.DeserializeObject<T>(content);
        }

        private static bool IsSeasonLink(HtmlNode node) =>
            node.Attributes.Any(a => a.Name == "href" && a.Value.Contains("season") && !a.Value.Contains("-1")) 
            && !node.InnerText.ToLower().Equals("unknown")
            && !node.InnerText.ToLower().Contains("see all");
    }
}