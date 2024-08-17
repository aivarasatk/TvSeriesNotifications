using Moq;
using System;
using System.Linq;
using TVSeriesNotifications.Core.DateTimeProvider;
using TVSeriesNotifications.Domain.Ports.HtmlParser;
using TVSeriesNotifications.Infrastructure.Adapters.HtmlParser;
using TVSeriesNotifications.Infrastructure.Adapters.HtmlParser.Exceptions;
using Xunit;

namespace TVSeriesNotifications.Tests.BusinessLogic
{
    public class HtmlParserV2Tests
    {
        private readonly IHtmlParser _parser;

        public HtmlParserV2Tests()
        {
            var dateTimeMock = new Mock<IDateTimeProvider>();
            dateTimeMock.Setup(provider => provider.Now).Returns(new DateTime(2023,01,02, 0, 0, 0, DateTimeKind.Utc));
            _parser = new HtmlParserV2(dateTimeMock.Object);
        }

        [Fact]
        public void GivenValidSingleSeasonHtml_OneNodeIsReturned()
        {
            // Arrange && Act
            var season = _parser.Seasons(ValidSingleSeasonNodeHtml);

            //Assert
            Assert.Single(season);
        }

        [Fact]
        public void GivenValidMultiSeasonHtml_MultipleNodesAreReturned()
        {
            // Arrange && Act
            var season = _parser.Seasons(ValidMultiSeasonNodesHtml);

            // Assert
            Assert.True(season.Count() > 1);
        }

        [Fact]
        public void GivenValidMultiSeasonHtml_OnlyNumericNodesAreReturned()
        {
            // Arrange && Act
            var seasons = _parser.Seasons(ValidMultiSeasonNodesHtml);

            // Assert
            Assert.True(seasons.Count() is 2
                && seasons.Contains(1)
                && seasons.Contains(2));
        }

        [Fact]
        public void GivenHtmlWithMissingRootSeasonsNode_Throws()
        {
            // Arrange && Act && Assert
            Assert.Throws<ImdbHtmlChangedException>(() => _parser.Seasons(MissingRootSeasonNodeHtml));
        }

        [Fact]
        public void GivenHtmlWithMissingSeasonOptions_Throws()
        {
            // Arrange && Act && Assert
            Assert.Throws<ImdbHtmlChangedException>(() => _parser.Seasons(MissingSeasonOptionsHtml));
        }

        [Fact]
        public void GivenHtmlWithMissingDateRange_Throws()
        {
            // Arrange && Act && Assert
            Assert.Throws<ImdbHtmlChangedException>(() => _parser.ShowIsCancelled(MissingDateRangeHtml));
        }

        [Fact]
        public void GivenHtmlWithoutDateRangeSeparator_ShowIsCancelled()
        {
            // Arrange && Act
            var isCancelled = _parser.ShowIsCancelled(MissingDateRangeSeparatorHtml);

            // Assert
            Assert.True(isCancelled);
        }

        [Theory]
        [InlineData("2010–", false)] // long dash '–'
        [InlineData("2010–2011", true)]
        public void GivenValidDateRanges_ReturnsCancelationStatus(string range, bool isCancelled)
        {
            // Arrange && Act
            var result = _parser.ShowIsCancelled(DateRangeHtmlBuilder(range));

            // Assert
            Assert.Equal(isCancelled, result);
        }

        [Theory]
        [InlineData("Fri, Feb 4, 2022", true)]
        [InlineData("2023", false)]
        [InlineData("Fri, Feb 4, 2024", false)]
        [InlineData("2024", false)]
        [InlineData("Dec 2024", false)]
        [InlineData("", false)]
        public void GivenSeasonPageWithAirDateContents_ReturnsWhetherSeasonHasAired(string dateText, bool hasAired)
        {
            // Arrange && Act
            var result = _parser.AnyEpisodeHasAired(SeasonPageWithAirDateHtmlBuilder(dateText));

            // Assert
            Assert.Equal(hasAired, result);
        }

        [Fact]
        public void GivenSeasonPageWithUnairedEpisodeHtml_ReturnsSeasonUnaired()
        {
            // Arrange && Act
            var result = _parser.AnyEpisodeHasAired(SeasonPageWithUnairedEpisodeHtml());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GivenSeasonPageWithAiredEpisodeHtml_ReturnsSeasonAired()
        {
            // Arrange && Act
            var result = _parser.AnyEpisodeHasAired(SeasonPageWithAiredEpisodeHtml());

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GivenSeasonPageWithMissingAirDateEpisodeHtml_Throws()
        {
            //Arrange && Act Assert
            Assert.Throws<ImdbHtmlChangedException>(() => _parser.AnyEpisodeHasAired(SeasonPageWithMissingAirDateEpisodeHtml()));
        }

        [Fact]
        public void GivenSeasonPageWithMissingEpisodeRatingHtml_Throws()
        {
            //Arrange && Act Assert
            Assert.Throws<ImdbHtmlChangedException>(() => _parser.AnyEpisodeHasAired(SeasonPageWithMissingEpisodeRatingHtml()));
        }

        private const string ValidSingleSeasonNodeHtml = @"
        <span class=""ipc-btn__text"">1 Season</span>";

        private const string ValidMultiSeasonNodesHtml = @"
        <select id=""browse-episodes-season"">
            <option selected value></option>
            <option value=""2"">2</option>
            <option value=""1"">1</option>
            <option value=""SEE_ALL"">See all</option>
        </select>";

        private const string MissingRootSeasonNodeHtml = @"
        <select id=""MISSING"">
            <option value=""2"">2</option>
            <option value=""1"">1</option>
        </select>";

        private const string MissingSeasonOptionsHtml = @"
        <select id=""browse-episodes-season"">
            <season value=""2"">2</season>
        </select>";

        private const string MissingDateRangeHtml = @"
        <span></span>";

        private const string MissingDateRangeSeparatorHtml = @"
        <a class=""ipc-link ipc-link--baseAlt ipc-link--inherit-color"">
            2010
        </a>";

        private string DateRangeHtmlBuilder(string range)
        {
            return @$"
            <a class=""ipc-link ipc-link--baseAlt ipc-link--inherit-color"">
                {range}
            </a>";
        }

        private string SeasonPageWithAirDateHtmlBuilder(string dateText)
        {
            return @$"
                    <html>
                    <span class=""sc-ccd6e31b-10 dYquTu"">{dateText}</span>

                    <div class=""sc-e2dbc1a3-0 jeHPdh sc-282bae8e-3 eJhLqU"">
                        <span/><button/>
                    </div>
                    </html>
                    ";
        }

        private string SeasonPageWithUnairedEpisodeHtml()
        {
            return @$"
            <html>
            <div class=""sc-e2dbc1a3-0 jeHPdh sc-282bae8e-3 eJhLqU""></div>
            </html>
            ";
        }

        private string SeasonPageWithAiredEpisodeHtml()
        {
            return @$"
            <html>
            <div class=""sc-e2dbc1a3-0 jeHPdh sc-282bae8e-3 eJhLqU"">
                <span></span><button></button>
            </div>

            <span class=""sc-ccd6e31b-10 dYquTu"">Fri, Feb 4, 2022</span>
            </html>
            ";
        }

        private string SeasonPageWithMissingAirDateEpisodeHtml()
        {
            return @$"
            <html>
            <div class=""sc-e2dbc1a3-0 jeHPdh sc-282bae8e-3 eJhLqU"">
                <span/><button/>
            </div>
            </html>
            ";
        }

        private string SeasonPageWithMissingEpisodeRatingHtml()
        {
            return @$"<html></html>";
        }

    }
}
