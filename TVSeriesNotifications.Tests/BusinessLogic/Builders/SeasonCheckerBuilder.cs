using Moq;
using TVSeriesNotifications.Api;
using TVSeriesNotifications.BusinessLogic;
using TVSeriesNotifications.DateTimeProvider;
using TVSeriesNotifications.Persistance;

namespace TVSeriesNotifications.Tests.BusinessLogic.Builders
{
    public class SeasonCheckerBuilder
    {
        private IImdbClient _client;
        private IHtmlParser _htmlParser;
        private IPersistantCache<string> _cacheTvShowIds;
        private IPersistantCache<string> _cacheIgnoredTvShows;
        private IPersistantCache<int> _cacheLatestAiredSeasons;
        private IDateTimeProvider _dateTimeProvider;

        public SeasonCheckerBuilder()
        {
            _client = new Mock<IImdbClient>().Object;
            _htmlParser = new Mock<IHtmlParser>().Object;
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

        public SeasonCheckerBuilder WithHtmlParser(IHtmlParser parser)
        {
            _htmlParser = parser;
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
                _htmlParser,
                _cacheTvShowIds,
                _cacheIgnoredTvShows,
                _cacheLatestAiredSeasons,
                _dateTimeProvider);
        }
    }
}
