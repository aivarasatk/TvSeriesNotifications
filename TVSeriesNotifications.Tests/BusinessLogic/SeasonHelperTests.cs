using System;
using System.Threading.Tasks;
using TVSeriesNotifications.Domain.Models;
using Xunit;

namespace TVSeriesNotifications.Tests.BusinessLogic
{
    public class SeasonHelperTests
    {
        [Theory]
        [InlineData(2019, "2019")]
        [InlineData(2019, "2019-2020")]
        public async Task EndedTvSeries_AreNotOngoing(int yearStart, string yearRange)
        {
            // Arrange
            var input = new TvShow("", "", TVCategory.TVSeries, yearStart, yearRange);

            // Act
            var result = SeasonHelper.ShowIsOnGoing(input, DateTime.Now);

            // Assert
            Assert.False(result);
        }

        // Ongoing examples 2018-, 2100- (upcoming), 2018-2100(ends in current year + n years)
        [Theory]
        [InlineData(2019, "2019-")]
        [InlineData(2100, "2100-")]
        [InlineData(2019, "2019-2100")]
        public async Task LiveTvSeries_AreOngoing(int yearStart, string yearRange)
        {
            // Arrange
            var input = new TvShow("", "", TVCategory.TVSeries, yearStart, yearRange);

            // Act
            var result = SeasonHelper.ShowIsOnGoing(input, DateTime.Now);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TvSeriesEndingThisYear_AreOngoing()
        {
            // Arrange
            var input = new TvShow("", "", TVCategory.TVSeries, 2010, $"2010-{DateTime.Now.Year}");

            // Act
            var result = SeasonHelper.ShowIsOnGoing(input, DateTime.Now);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task EndedTvMiniSeries_AreNotOngoing()
        {
            // Arrange
            var input = new TvShow("", "", TVCategory.TVMiniSeries, 2010, $"2010-2010");

            // Act
            var result = SeasonHelper.ShowIsOnGoing(input, DateTime.Now);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(2100)]
        [InlineData(2022)]// "today's year"
        public async Task UpcomingTvMiniSeries_AreOngoing(int year)
        {
            // Arrange
            var input = new TvShow("", "", TVCategory.TVMiniSeries, year, $"{year}-{year}");

            // Act
            var result = SeasonHelper.ShowIsOnGoing(input, new DateTime(2022, 10, 10));

            // Assert
            Assert.True(result);
        }
    }
}
