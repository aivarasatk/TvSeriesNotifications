using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading.Tasks;
using SafeParallel;
using TVSeriesNotifications.Api;
using TVSeriesNotifications.BusinessLogic;
using TVSeriesNotifications.CustomExceptions;
using TVSeriesNotifications.Notifications;
using TVSeriesNotifications.Persistance;

namespace TVSeriesNotifications
{
    public static class Program
    {
        private static readonly ISeasonChecker _seasonChecker;
        private static readonly INotificationService _notificationService;
        private static readonly ITvShowRepository _tvShowRepository;

        static Program()
        {
            var client = new ImdbClient();
            var cacheTvShowIds = new PersistantCache<string>("Cache/TvShowIds");
            var cacheIgnoredTvShows = new PersistantCache<string>("Cache/IgnoredTvShows");
            var cacheLatestAiredSeasons = new PersistantCache<int>("Cache/LatestAiredSeasons");
            _seasonChecker = new SeasonChecker(client, cacheTvShowIds, cacheIgnoredTvShows, cacheLatestAiredSeasons);

            _notificationService = new FileNotificationService();
            _tvShowRepository = new FileTvShowRepository(new FileSystem());
        }

        public static async Task Main()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var tvShows = await _tvShowRepository.RetrieveTvShows().ConfigureAwait(false);
                await CheckForNewSeasonsAsync(tvShows).ConfigureAwait(false);
            }
            catch (ImdbHtmlChangedException ihce)
            {
                await _notificationService.NotifyAboutErrorsAsync($"{DateTime.Now}: HTML changed: {ihce.Message}");
            }
            catch (Exception ex)
            {
                await _notificationService.NotifyAboutErrorsAsync($"{DateTime.Now}: Unexpected exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }

            Console.WriteLine($"Elapsed: {stopwatch.Elapsed.TotalSeconds}");
            Console.ReadKey();
        }

        private static async Task CheckForNewSeasonsAsync(IEnumerable<string> tvShows)
        {
            await tvShows.SafeParallelAsync(async tvShow =>
            {
                var (newSeasonAired, newSeason) = await _seasonChecker.TryCheckForNewSeasonAsync(tvShow);
                if (newSeasonAired)
                {
                    await _notificationService.NotifyNewSeasonAsync(newSeason);
                }
            }).ConfigureAwait(false);
        }
    }
}