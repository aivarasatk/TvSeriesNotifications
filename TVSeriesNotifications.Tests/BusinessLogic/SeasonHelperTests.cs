using System;
using System.Threading.Tasks;
using TVSeriesNotifications.Domain.Models;
using Xunit;

namespace TVSeriesNotifications.Tests.BusinessLogic
{
    public class SeasonHelperTests
    {
        [Theory]
        [InlineData(2019, "2019-2020")]
        [InlineData(2019, "2019-2019")]
        public void EndedTvSeries_AreNotOngoing(int yearStart, string yearRange)
        {
            // Arrange
            var input = new TvShow(Id: "", Title: "", TVCategory.TVSeries, yearStart, yearRange);

            // Act
            var result = SeasonHelper.ShowIsOnGoing(input, DateTime.Now);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(2019, null)]
        [InlineData(2100, null)]
        [InlineData(2019, "2019-2100")]
        public void LiveTvSeries_AreOngoing(int yearStart, string yearRange)
        {
            // Arrange
            var input = new TvShow(Id: "", Title: "", TVCategory.TVSeries, yearStart, yearRange);

            // Act
            var result = SeasonHelper.ShowIsOnGoing(input, DateTime.Now);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TvSeriesEndingThisYear_AreOngoing()
        {
            // Arrange
            var input = new TvShow(Id: "", Title: "", TVCategory.TVSeries, 2010, $"2010-{DateTime.Now.Year}");

            // Act
            var result = SeasonHelper.ShowIsOnGoing(input, DateTime.Now.Date);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void EndedTvMiniSeries_AreNotOngoing()
        {
            // Arrange
            var input = new TvShow(Id: "", Title: "", TVCategory.TVMiniSeries, 2010, "2010-2010");

            // Act
            var result = SeasonHelper.ShowIsOnGoing(input, DateTime.Now);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(2100)]
        [InlineData(2022)]// "today's year"
        [InlineData(2023)]
        public void UpcomingTvMiniSeries_AreOngoing(int year)
        {
            // Arrange
            var input = new TvShow(Id: "", Title: "", TVCategory.TVMiniSeries, year, $"{year}-{year}");

            // Act
            var result = SeasonHelper.ShowIsOnGoing(input, new DateTime(2022, 01, 01));

            // Assert
            Assert.True(result);
        }
    }
}
