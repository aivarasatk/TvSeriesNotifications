using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TVSeriesNotifications.Tests.BusinessLogic.Builders;
using TVSeriesNotifications.Tests.Fakes.Persistance;
using TVSeriesNotifications.Core.DateTimeProvider;
using TVSeriesNotifications.Domain.Ports.HtmlParser;
using TVSeriesNotifications.Infrastructure.Adapters.HtmlParser.Exceptions;
using Xunit;
using TVSeriesNotifications.Domain.Ports.Repository;
using TVSeriesNotifications.Domain.Ports.ImdbClient;
using TVSeriesNotifications.Domain.Models;
using System.Linq;

namespace TVSeriesNotifications.Tests.BusinessLogic
{
    public class SeasonCheckerTests
    {
        [Fact]
        public async Task When_TvShowIsMarkedAsIgnored_NewSeasonIsNotAired()
        {
            // Arrange
            var ignoredTvShowsCache = new Mock<IPersistantCache<string>>();
            ignoredTvShowsCache.Setup(m => m.Exists(It.IsAny<string>()))
                .Returns(true);

            var sut = new SeasonCheckerBuilder()
                .WithCacheIgnoredTvShows(ignoredTvShowsCache.Object)
                .Build();

            // Act
            var (success, newSeason) = await sut.TryCheckForNewSeasonAsync("The Blacklist");

            // Assert
            Assert.False(success);
        }

        [Fact]
        public async Task When_TvShowSuggestionsIsNotFound_TvShowIsAddedToIgnoredList()
        {
            // Arrange
            var imdbClient = new Mock<IImdbClient>();
            imdbClient.Setup(m => m.GetSuggestionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(Enumerable.Empty<TvShow>()));

            var ignoredTvShowsCache = new FakePersistantCache<string>();

            var sut = new SeasonCheckerBuilder()
                .WithCacheIgnoredTvShows(ignoredTvShowsCache)
                .WithImdbClient(imdbClient.Object)
                .Build();

            // Act
            var (success, newSeason) = await sut.TryCheckForNewSeasonAsync("The Blacklist");

            // Assert
            Assert.False(success);
            Assert.NotEmpty(ignoredTvShowsCache.CacheItems);
        }

        [Theory]
        [InlineData("2015-2019")]
        [InlineData("2019-2019")]
        public async Task When_TvShowIsNotOngoing_TvShowIsAddedToIgnoredList(string yearRange)
        {
            // Arrange
            IEnumerable<TvShow> suggestions = new TvShow[]
            {
                new ("", "The Blacklist", TVCategory.TVSeries, 2015, yearRange)
            };

            var imdbClient = new Mock<IImdbClient>();
            imdbClient.Setup(m => m.GetSuggestionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(suggestions));

            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(m => m.Now).Returns(new DateTime(2020, 1, 1));

            var ignoredTvShowsCache = new Fakes.Persistance.FakePersistantCache<string>();

            var sut = new SeasonCheckerBuilder()
                .WithCacheIgnoredTvShows(ignoredTvShowsCache)
                .WithImdbClient(imdbClient.Object)
                .WithDateTimeProvider(dateTimeProvider.Object)
                .Build();

            // Act
            var (success, newSeason) = await sut.TryCheckForNewSeasonAsync("The Blacklist");

            // Assert
            Assert.False(success);
            Assert.NotEmpty(ignoredTvShowsCache.CacheItems);
        }

        [Theory]
        [InlineData(TVCategory.TVSeries, null)]
        [InlineData(TVCategory.TVMiniSeries, "2022-2022")]
        [InlineData(TVCategory.TVMiniSeries, "2023-2023")]
        public async Task When_TvShowIsToBeAired_ItIsAddedToSubscriptionList(TVCategory category, string yearRange)
        {
            // Arrange
            var newTvShow = "New Tv Show";

            IEnumerable<TvShow> suggestions = new TvShow[]
            {
                new ("", newTvShow, category, 2023, yearRange)
            };

            var imdbClient = new Mock<IImdbClient>();
            imdbClient.Setup(m => m.GetSuggestionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(suggestions));

            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(m => m.Now).Returns(new DateTime(2022, 1, 1)); // Stubbed suggestion is now "to be aired"

            var htmlParser = new Mock<IHtmlParser>();
            htmlParser.Setup(h => h.Seasons(It.IsAny<string>()))
                .Returns(new[]
                {
                    1
                });

            var htmlParserStrategyFactory = new Mock<IHtmlParserStrategyFactory>();
            htmlParserStrategyFactory.Setup(h => h.ResolveParsingStrategy(It.IsAny<string>()))
                .Returns(htmlParser.Object);

            var latestAiredSeason = new FakePersistantCache<int>();

            var sut = new SeasonCheckerBuilder()
                .WithCacheLatestAiredSeasons(latestAiredSeason)
                .WithImdbClient(imdbClient.Object)
                .WithHtmlParserStrategy(htmlParserStrategyFactory.Object)
                .WithDateTimeProvider(dateTimeProvider.Object)
                .Build();

            // Act
            var (success, newSeason) = await sut.TryCheckForNewSeasonAsync(newTvShow);

            // Assert
            Assert.False(success); // upcoming tv show is treated as a new ongoing tv show. Meaning we now have "latest" aired season
            Assert.True(latestAiredSeason.CacheItems.ContainsKey(newTvShow));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("2020-2100")]
        public async Task When_AnOngoingTvShowIsFirstChecked_LatestAiredSeasonIsSet(string yearRange)
        {
            // Arrange
            var newTvShow = "The Blacklist";

            IEnumerable<TvShow> suggestions = new TvShow[]
            {
                new ("", newTvShow, TVCategory.TVSeries, 2020, yearRange)
            };

            var imdbClient = new Mock<IImdbClient>();
            imdbClient.Setup(m => m.GetSuggestionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(suggestions));

            var htmlParser = new Mock<IHtmlParser>();
            htmlParser.Setup(p => p.Seasons(It.IsAny<string>()))
                .Returns(new[]
                {
                    1,
                    2
                });

            var htmlParserStrategyFactory = new Mock<IHtmlParserStrategyFactory>();
            htmlParserStrategyFactory.Setup(h => h.ResolveParsingStrategy(It.IsAny<string>()))
                .Returns(htmlParser.Object);

            htmlParser.Setup(p => p.AnyEpisodeHasAired(It.IsAny<string>()))
                .Returns(true);

            var latestAiredTvShowCache = new FakePersistantCache<int>();

            var sut = new SeasonCheckerBuilder()
                .WithImdbClient(imdbClient.Object)
                .WithHtmlParserStrategy(htmlParserStrategyFactory.Object)
                .WithCacheLatestAiredSeasons(latestAiredTvShowCache)
                .Build();

            // Act
            var (success, newSeason) = await sut.TryCheckForNewSeasonAsync(newTvShow);

            // Assert
            Assert.True(latestAiredTvShowCache.Exists(newTvShow));
        }

        [Fact]
        public async Task When_NoSeasonsAreReturnedForANewShow_HtmlChangedExceptionThrows()
        {
            // Arrange
            var newTvShow = "The Blacklist";

            IEnumerable<TvShow> suggestions = new TvShow[]
            {
                new ("", newTvShow, TVCategory.TVSeries, 2020, null)
            };

            var imdbClient = new Mock<IImdbClient>();
            imdbClient.Setup(m => m.GetSuggestionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(suggestions));

            var htmlParserStrategyFactory = new Mock<IHtmlParserStrategyFactory>();
            htmlParserStrategyFactory.Setup(h => h.ResolveParsingStrategy(It.IsAny<string>()))
                .Returns(new Mock<IHtmlParser>().Object);

            var latestAiredTvShowCache = new FakePersistantCache<int>();

            var sut = new SeasonCheckerBuilder()
                .WithImdbClient(imdbClient.Object)
                .WithCacheLatestAiredSeasons(latestAiredTvShowCache)
                .WithHtmlParserStrategy(htmlParserStrategyFactory.Object)
                .Build();

            // Act & Assert
            await Assert.ThrowsAsync<ImdbHtmlChangedException>(() => sut.TryCheckForNewSeasonAsync(newTvShow));
        }

        [Fact]
        public async Task When_TvShowHasNoUpcomingOrAiredSeason_HtmlChangedExceptionThrows()
        {
            // Arrange
            var newTvShow = "The Blacklist";

            IEnumerable<TvShow> suggestions = new TvShow[]
            {
                new ("", newTvShow, TVCategory.TVSeries, 2020, null)
            };

            var imdbClient = new Mock<IImdbClient>();
            imdbClient.Setup(m => m.GetSuggestionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(suggestions));

            var latestAiredTvShowCache = new FakePersistantCache<int>();

            var htmlParser = new Mock<IHtmlParser>();
            htmlParser.Setup(p => p.Seasons(It.IsAny<string>()))
                .Returns(new[]
                {
                    1,
                    2
                });

            var htmlParserStrategyFactory = new Mock<IHtmlParserStrategyFactory>();
            htmlParserStrategyFactory.Setup(h => h.ResolveParsingStrategy(It.IsAny<string>()))
                .Returns(htmlParser.Object);

            var sut = new SeasonCheckerBuilder()
                .WithImdbClient(imdbClient.Object)
                .WithHtmlParserStrategy(htmlParserStrategyFactory.Object)
                .WithCacheLatestAiredSeasons(latestAiredTvShowCache)
                .Build();

            // Act & Assert
            await Assert.ThrowsAsync<ImdbHtmlChangedException>(() => sut.TryCheckForNewSeasonAsync(newTvShow));
        }

        [Fact]
        public async Task When_TvShowIsCancelled_ShowIsSetAsIgnored()
        {
            // Arrange
            var tvShow = "The Blacklist";

            var tvShowIdCache = new FakePersistantCache<string>(
                new Dictionary<string, string> { [tvShow] = "some id" });

            var latestAiredSeasonCache = new FakePersistantCache<int>(
                new Dictionary<string, int> { [tvShow] = 3 });

            var ignoredTvShowCache = new FakePersistantCache<string>();

            var htmlParser = new Mock<IHtmlParser>();

            // Setup with no new season.
            htmlParser.Setup(p => p.Seasons(It.IsAny<string>()))
                .Returns(new[]
                {
                    3
                });

            htmlParser.Setup(p => p.ShowIsCancelled(It.IsAny<string>()))
                .Returns(true);

            var htmlParserStrategyFactory = new Mock<IHtmlParserStrategyFactory>();
            htmlParserStrategyFactory.Setup(h => h.ResolveParsingStrategy(It.IsAny<string>()))
                .Returns(htmlParser.Object);

            var sut = new SeasonCheckerBuilder()
                .WithCacheTvShowIds(tvShowIdCache)
                .WithCacheLatestAiredSeasons(latestAiredSeasonCache)
                .WithCacheIgnoredTvShows(ignoredTvShowCache)
                .WithHtmlParserStrategy(htmlParserStrategyFactory.Object)
                .Build();

            // Act & Assert
            var (newSeasonAired, seasonInfo) = await sut.TryCheckForNewSeasonAsync(tvShow);

            Assert.False(newSeasonAired);

            Assert.True(ignoredTvShowCache.CacheItems.ContainsKey(tvShow));
            Assert.False(latestAiredSeasonCache.CacheItems.ContainsKey(tvShow));
            Assert.False(tvShowIdCache.CacheItems.ContainsKey(tvShow));
        }

        [Fact]
        public async Task When_TvShowHasANewAiredSeason_ShowCacheIsUpdatedWithTheValue()
        {
            // Arrange
            var tvShow = "The Blacklist";

            var tvShowIdCache = new FakePersistantCache<string>(
                new Dictionary<string, string> { [tvShow] = "some id" });

            var latestAiredSeasonCache = new FakePersistantCache<int>(
                new Dictionary<string, int> { [tvShow] = 3 });

            var htmlParser = new Mock<IHtmlParser>();

            // Setup with new season.
            htmlParser.Setup(p => p.Seasons(It.IsAny<string>()))
                .Returns(new[]
                {
                    4
                });

            htmlParser.Setup(p => p.AnyEpisodeHasAired(It.IsAny<string>()))
                .Returns(true);

            var htmlParserStrategyFactory = new Mock<IHtmlParserStrategyFactory>();
            htmlParserStrategyFactory.Setup(h => h.ResolveParsingStrategy(It.IsAny<string>()))
                .Returns(htmlParser.Object);

            var sut = new SeasonCheckerBuilder()
                .WithCacheTvShowIds(tvShowIdCache)
                .WithCacheLatestAiredSeasons(latestAiredSeasonCache)
                .WithHtmlParserStrategy(htmlParserStrategyFactory.Object)
                .Build();

            // Act & Assert
            var (newSeasonAired, seasonInfo) = await sut.TryCheckForNewSeasonAsync(tvShow);

            Assert.True(newSeasonAired);
            Assert.Equal(4, latestAiredSeasonCache.CacheItems[tvShow]);
        }

        [Fact]
        public async Task When_TvShowHasNoNewAiredSeason_CacheStateIsNotChanged()
        {
            // Arrange
            var tvShow = "The Blacklist";

            var tvShowIdCache = new FakePersistantCache<string>(
                new Dictionary<string, string> { [tvShow] = "some id" });

            var latestAiredSeasonCache = new FakePersistantCache<int>(
                new Dictionary<string, int> { [tvShow] = 3 });

            var ignoredTvShowCache = new FakePersistantCache<string>();

            var htmlParser = new Mock<IHtmlParser>();

            htmlParser.Setup(p => p.Seasons(It.IsAny<string>()))
                .Returns(new[]
                {
                    4
                });

            htmlParser.Setup(p => p.ShowIsCancelled(It.IsAny<string>()))
                .Returns(false);

            var htmlParserStrategyFactory = new Mock<IHtmlParserStrategyFactory>();
            htmlParserStrategyFactory.Setup(h => h.ResolveParsingStrategy(It.IsAny<string>()))
                .Returns(htmlParser.Object);

            var sut = new SeasonCheckerBuilder()
                .WithCacheTvShowIds(tvShowIdCache)
                .WithCacheLatestAiredSeasons(latestAiredSeasonCache)
                .WithCacheIgnoredTvShows(ignoredTvShowCache)
                .WithHtmlParserStrategy(htmlParserStrategyFactory.Object)
                .Build();

            // Act & Assert
            var (newSeasonAired, seasonInfo) = await sut.TryCheckForNewSeasonAsync(tvShow);

            Assert.False(newSeasonAired);

            Assert.True(tvShowIdCache.CacheItems.ContainsKey(tvShow));
            Assert.Equal(3, latestAiredSeasonCache.CacheItems[tvShow]);
            Assert.False(ignoredTvShowCache.CacheItems.ContainsKey(tvShow));
        }
    }
}
