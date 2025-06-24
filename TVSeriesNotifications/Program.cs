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
    public class Program
    {
        private static ISeasonChecker _seasonChecker;
        private static INotificationService _notificationService;
        private static ITvShowRepository _tvShowRepository;


        private static async Task<HtmlElement> ResolveSeasonAirDateElementAsync(ImdbClient client)
        {
            var outlanderTvShowId = "tt3006802";
            var outlanderSeasonOne = await client.GetSeasonPageContentsAsync(outlanderTvShowId, 1);
            var htmlElement = HtmlParserBase.TryResolveHtmlElement(outlanderSeasonOne, "Sat, Aug 9, 2014");
            return htmlElement;
        }

        public static async Task Main()
        {
            var client = new ImdbClient();
            HtmlElement airDateElement;
            try
            {
                airDateElement = await ResolveSeasonAirDateElementAsync(client);
            }
            catch
            {
                _notificationService.NotifyAboutErrors($"{DateTime.Now}: could not find baseline for air date HTML element");
                return;
            }

            var dateTimeProvider = new DateTimeProvider();
            var htmlParser = new HtmlParserV2(dateTimeProvider, airDateElement);
            var cacheTvShowIds = new PersistantCache<string>("Cache/TvShowIds");
            var cacheIgnoredTvShows = new PersistantCache<string>("Cache/IgnoredTvShows");
            var cacheLatestAiredSeasons = new PersistantCache<int>("Cache/LatestAiredSeasons");
            _seasonChecker = new SeasonChecker(client, htmlParser, cacheTvShowIds, cacheIgnoredTvShows, cacheLatestAiredSeasons, dateTimeProvider);

            _notificationService = new FileNotificationService();
            _tvShowRepository = new FileTvShowRepository(new FileSystem());

            var stopwatch = Stopwatch.StartNew();
            var tvShows = await _tvShowRepository.RetrieveTvShows();
            await CheckForNewSeasonsAsync(tvShows);

            Console.WriteLine($"Elapsed: {stopwatch.Elapsed.TotalSeconds}");
        }

        private static async Task CheckForNewSeasonsAsync(IEnumerable<string> tvShows)
        {
            var worker = new ActionBlock<string>(async tvShow =>
            {
                try
                {
                    var (newSeasonAired, newSeason) = await _seasonChecker.TryCheckForNewSeasonAsync(tvShow);
                    if (newSeasonAired)
                    {
                        _notificationService.NotifyNewSeason(newSeason);
                    }
                }
                catch (ImdbHtmlChangedException ihce)
                {
                    _notificationService.NotifyAboutErrors($"{DateTime.Now}: HTML changed exception ({ihce.InnerException}): {ihce.Message}{Environment.NewLine}{ihce.StackTrace}");
                }
                catch (Exception ex)
                {
                    _notificationService.NotifyAboutErrors($"{DateTime.Now}: Unexpected exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
            }, 
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = -1,
                BoundedCapacity = 10
            });

            foreach (var tvShow in tvShows)
                await worker.SendAsync(tvShow);

            worker.Complete();
            await worker.Completion;
        }
    }
}