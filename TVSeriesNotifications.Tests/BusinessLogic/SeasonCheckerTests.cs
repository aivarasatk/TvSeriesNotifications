using Moq;
using System;
using System.Threading.Tasks;
using TVSeriesNotifications.Api;
using TVSeriesNotifications.DateTimeProvider;
using TVSeriesNotifications.Persistance;
using TVSeriesNotifications.Tests.BusinessLogic.Builders;
using Xunit;

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
                .Returns(Task.FromResult(new JsonModels.ImdbSuggestion()));

            var ignoredTvShowsCache = new Fakes.Persistance.FakePersistantCache<string>();

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
        [InlineData("2019")]
        public async Task When_TvShowIsNotOngoing_TvShowIsAddedToIgnoredList(string yearRange)
        {
            // Arrange
            var imdbClient = new Mock<IImdbClient>();
            imdbClient.Setup(m => m.GetSuggestionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new JsonModels.ImdbSuggestion
                {
                    Suggestions = new JsonModels.Suggestion[]
                    {
                        new ("TV series", "", "The Blacklist", 2015, yearRange)
                    }
                }));

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
    }
}
