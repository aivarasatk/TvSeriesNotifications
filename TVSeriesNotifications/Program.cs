using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TVSeriesNotifications.Api;
using TVSeriesNotifications.BusinessLogic;
using TVSeriesNotifications.Core.DateTimeProvider;
using TVSeriesNotifications.Domain.Ports.Notifications;
using TVSeriesNotifications.Domain.Ports.Repository;
using TVSeriesNotifications.Infrastructure.Adapters.HtmlParser;
using TVSeriesNotifications.Infrastructure.Adapters.HtmlParser.Exceptions;
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
            var dateTimeProvider = new DateTimeProvider();
            var htmlParserStrategy = new HtmlParserStrategyFactory();
            var cacheTvShowIds = new PersistantCache<string>("Cache/TvShowIds");
            var cacheIgnoredTvShows = new PersistantCache<string>("Cache/IgnoredTvShows");
            var cacheLatestAiredSeasons = new PersistantCache<int>("Cache/LatestAiredSeasons");
            _seasonChecker = new SeasonChecker(client, htmlParserStrategy, cacheTvShowIds, cacheIgnoredTvShows, cacheLatestAiredSeasons, dateTimeProvider);

            _notificationService = new FileNotificationService();
            _tvShowRepository = new FileTvShowRepository(new FileSystem());
        }

        public static async Task Main()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var tvShows = await _tvShowRepository.RetrieveTvShows();
                await CheckForNewSeasonsAsync(tvShows);
            }
            catch (ImdbHtmlChangedException ihce)
            {
                _notificationService.NotifyAboutErrors($"{DateTime.Now}: HTML changed exception ({ihce.InnerException}): {ihce.Message}{Environment.NewLine}{ihce.StackTrace}");
            }
            catch (Exception ex)
            {
                _notificationService.NotifyAboutErrors($"{DateTime.Now}: Unexpected exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }

            Console.WriteLine($"Elapsed: {stopwatch.Elapsed.TotalSeconds}");
        }

        private static async Task CheckForNewSeasonsAsync(IEnumerable<string> tvShows)
        {
            var worker = new ActionBlock<string>(async tvShow =>
            {
                var (newSeasonAired, newSeason) = await _seasonChecker.TryCheckForNewSeasonAsync(tvShow);
                if (newSeasonAired)
                {
                    _notificationService.NotifyNewSeason(newSeason);
                }
            }, 
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 50,
            });

            foreach (var tvShow in tvShows)
                await worker.SendAsync(tvShow);

            worker.Complete();
            await worker.Completion;
        }
    }
}