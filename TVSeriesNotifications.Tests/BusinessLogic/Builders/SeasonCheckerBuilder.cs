using Moq;
using TVSeriesNotifications.Api;
using TVSeriesNotifications.BusinessLogic;
using TVSeriesNotifications.Common;
using TVSeriesNotifications.DateTimeProvider;
using TVSeriesNotifications.Persistance;

namespace TVSeriesNotifications.Tests.BusinessLogic.Builders
{
    public class SeasonCheckerBuilder
    {
        private IImdbClient _client;
        private IHtmlParserStrategyFactory _htmlParserStrategy;
        private IPersistantCache<string> _cacheTvShowIds;
        private IPersistantCache<string> _cacheIgnoredTvShows;
        private IPersistantCache<int> _cacheLatestAiredSeasons;
        private IDateTimeProvider _dateTimeProvider;

        public SeasonCheckerBuilder()
        {
            _client = new Mock<IImdbClient>().Object;
            _htmlParserStrategy = new Mock<IHtmlParserStrategyFactory>().Object;
            _cacheTvShowIds = new Mock<IPersistantCache<string>>().Object;
            _cacheIgnoredTvShows = new Mock<IPersistantCache<string>>().Object;
            _cacheLatestAiredSeasons = new Mock<IPersistantCache<int>>().Object;
            _dateTimeProvider = new Mock<IDateTimeProvider>().Object;
        }

        public SeasonCheckerBuilder WithImdbClient(IImdbClient client)
        {
            _client = client;
            return this;
        }

        public SeasonCheckerBuilder WithHtmlParserStrategy(IHtmlParserStrategyFactory parserStrategy)
        {
            _htmlParserStrategy = parserStrategy;
            return this;
        }

        public SeasonCheckerBuilder WithCacheTvShowIds(IPersistantCache<string> cache)
        {
            _cacheTvShowIds = cache;
            return this;
        }

        public SeasonCheckerBuilder WithCacheIgnoredTvShows(IPersistantCache<string> cache)
        {
            _cacheIgnoredTvShows = cache;
            return this;
        }

        public SeasonCheckerBuilder WithCacheLatestAiredSeasons(IPersistantCache<int> cache)
        {
            _cacheLatestAiredSeasons = cache;
            return this;
        }

        public SeasonCheckerBuilder WithDateTimeProvider(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
            return this;
        }

        public SeasonChecker Build()
        {
            return new SeasonChecker(
                _client,
                _htmlParserStrategy,
                _cacheTvShowIds,
                _cacheIgnoredTvShows,
                _cacheLatestAiredSeasons,
                _dateTimeProvider);
        }
    }
}
