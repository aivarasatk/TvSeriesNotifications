using System.Linq;
using TVSeriesNotifications.BusinessLogic;
using TVSeriesNotifications.CustomExceptions;
using Xunit;

namespace TVSeriesNotifications.Tests.BusinessLogic
{
    public class HtmlParserTests
    {
		private readonly IHtmlParser _parser;

        public HtmlParserTests()
        {
			_parser = new HtmlParser(new DateTimeProvider.DateTimeProvider());
		}

        [Fact]
        public void GivenValidHtml_SeasonNodesAreReturned()
        {
			// Arrange & Act
			var result = _parser.SeasonNodes(ValidHtmlWith8SeasonNodes);

			Assert.NotEmpty(result);
        }

		[Fact]
		public void GivenValidHtml_SeasonNodesHaveCorrectSeasonValues()
		{
			// Arrange 
			var actualSeasons = new[] { "1", "2", "3", "4", "5", "6", "7", "8" };

			// Act
			var result = _parser.SeasonNodes(ValidHtmlWith8SeasonNodes).ToList();

			Assert.True(result.Count == 8);

			foreach (var season in actualSeasons)
				Assert.Contains(result, r => r.InnerText == season);
		}

		[Fact]
		public void GivenValidHtml_SeasonNodesHaveLinks()
		{
			// Arrange & Act
			var result = _parser.SeasonNodes(ValidHtmlWith8SeasonNodes).ToList();

			//Assert
			Assert.All(result,
				val => val.Attributes
				.Select(a => a)
				.Any(lnk => lnk.Name == "href" && lnk.Value.Contains("/title/tt2741602/episodes?season=")));
		}

		[Fact]
		public void GivenHtmlWithoutSeasonNode_SeasonNodesThrows()
		{
			// Arrange & Act & Assert
			Assert.Throws<ImdbHtmlChangedException>(() => _parser.SeasonNodes("<html></html>"));
		}

		[Fact]
		public void GivenHtmlWithoutLinksToSeason_SeasonNodesThrows()
		{
			// Arrange & Act & Assert
			Assert.Throws<ImdbHtmlChangedException>(() => _parser.SeasonNodes("<html><div class=\"seasons-and-year-nav\"></div></html>"));
		}

		[Fact]
		public void GivenValidShowEndedHtml_TvShowisCancelled()
		{
			// Arrange & Act
			var cancelled = _parser.ShowIsCancelled(ValidEndedTvShowHtml);

			//Assert
			Assert.True(cancelled);
		}

		[Fact]
		public void GivenInvalidShowEndedHtml_HtmlChangedExceptionThrown()
		{
			// Arrange & Act & Assert
			Assert.Throws<ImdbHtmlChangedException>(() => _parser.ShowIsCancelled("<div><a titile=\"See more\"></a></div>"));
		}

		[Theory]
		[InlineData("(2018-2019")]// long dash '�'
		[InlineData("2018-2019)")]
		[InlineData("2018-2019")]
		public void GivenShowEndedHtmlWithMissingBraces_HtmlChangedExceptionThrown_DueToMissingYearRange(string yearRange)
		{
			// Arrange & Act & Assert
			Assert.Throws<ImdbHtmlChangedException>(() => _parser.ShowIsCancelled($"<div><a title=\"See more release dates\">TV Series {yearRange}</a></div>"));
		}

		[Theory]
		[InlineData("(2018�)")]// long dash '�'
		[InlineData("(2018�2100)")]
		public void GivenValidOngoingShowHtml_ShowIsNotCancelled(string yearRange)
		{
			// Arrange & Act
			var cancelled = _parser.ShowIsCancelled($"<div><a title=\"See more release dates\">TV Series {yearRange}</a></div>");

			//Assert
			Assert.False(cancelled);
		}

		[Fact]
		public void ValidShowYearRangeRequiresADash()// long dash '�'
		{
			// Arrange & Act & Assert
			Assert.Throws<ImdbHtmlChangedException>(() => _parser.ShowIsCancelled($"<div><a title=\"See more release dates\">TV Series (2018)</a></div>"));
		}

		[Fact]
		public void EpisodeAirDateParsesSuccessfullyForMonthMay() // May is not abrieviated like Sep. or Jun.
		{
			// Arrange & Act
			var aired = _parser.AnyEpisodeHasAired(SeasonAirDatesForMay);

			// Assert
			Assert.True(aired);
		}

		[Fact]
		public void When_AirDates_AreInThePast_EpisodesHaveAired() // May is not abrieviated like Sep. or Jun.
		{
			// Arrange & Act
			var aired = _parser.AnyEpisodeHasAired(PastSeasonAirDates);

			// Assert
			Assert.True(aired);
		}

		[Fact]
		public void When_AirDates_AreInTheFuture_EpisodesHaveNotAired() // May is not abrieviated like Sep. or Jun.
		{
			// Arrange & Act
			var aired = _parser.AnyEpisodeHasAired(FutureSeasonAirDates);

			// Assert
			Assert.False(aired);
		}

		[Fact]
		public void When_AirDates_AreNotInHtml_Throws() // May is not abrieviated like Sep. or Jun.
		{
			// Arrange & Act & Assert
			Assert.Throws<ImdbHtmlChangedException>(() => _parser.AnyEpisodeHasAired($"<div><a title=\"See more release dates\">TV Series (2018)</a></div>"));
		}

		[Theory]
		[InlineData("")]
		[InlineData("2020")]
		[InlineData("2100")]
		public void When_AirDates_AreNotValid_EpisodesAreConsideredNotAired(string airDate) // May is not abrieviated like Sep. or Jun.
		{
			// Arrange & Act
			var aired = _parser.AnyEpisodeHasAired($"<div class=\"airdate\">{airDate}</div>");

			// Assert
			Assert.False(aired);
		}

		private const string ValidHtmlWith8SeasonNodes =
		@"<html>
			<div></div>
			<div/>
			<div>
				<div class=""seasons-and-year-nav"">
					<div>
						<h4 class=""float-left"">Seasons</h4>
						<hr />
					</div>
					<br class=""clear"" />
					<div>
						<a href=""/title/tt2741602/episodes?season=8&ref_=tt_eps_sn_8"">8</a>&nbsp;&nbsp;
						<a href=""/title/tt2741602/episodes?season=7&ref_=tt_eps_sn_7"">7</a>&nbsp;&nbsp;
						<a href=""/title/tt2741602/episodes?season=6&ref_=tt_eps_sn_6"">6</a>&nbsp;&nbsp;
						<a href=""/title/tt2741602/episodes?season=5&ref_=tt_eps_sn_5"">5</a>&nbsp;&nbsp;
						<a href=""/title/tt2741602/episodes?season=4&ref_=tt_eps_sn_4"">4</a>&nbsp;&nbsp;
						<a href=""/title/tt2741602/episodes?season=3&ref_=tt_eps_sn_3"">3</a>&nbsp;&nbsp;
						<a href=""/title/tt2741602/episodes?season=2&ref_=tt_eps_sn_2"">2</a>&nbsp;&nbsp;
						<a href=""/title/tt2741602/episodes?season=1&ref_=tt_eps_sn_1"">1</a>&nbsp;&nbsp;
					</div>
					<div>
						<a href=""/title/tt2741602/episodes?year=2021&ref_=tt_eps_yr_2021"">2021</a>&nbsp;&nbsp;
					</div>
				</div>
			</div>
		</html>";

		private const string ValidEndedTvShowHtml =
		@"
		<html>
			<div class=""primary_ribbon"">
				<div class=""ribbonize"" data-tconst=""tt0944947""
					data-caller-name=""title""></div>
				<div class=""wlb_dropdown_btn"" data-tconst=""tt0944947""
					data-size=""large"" data-caller-name=""title""
					data-type=""primary""></div>
				<div class=""wlb_dropdown_list"" style=""display:none""></div>
			</div>

			<div class=""title_wrapper"">
				<h1 class="""">Sostu karai&nbsp; </h1>
				<div class=""originalTitle"">Game of
					Thrones<span class=""description""> (original title)</span>
				</div>
				<div class=""subtext"">
					<time datetime=""PT57M"">
						57min
					</time>
					<span class=""ghost"">|</span>
					<a
						href=""/search/title?genres=action&explore=title_type,genres&ref_=tt_ov_inf"">Action</a>,
					<a
						href=""/search/title?genres=adventure&explore=title_type,genres&ref_=tt_ov_inf"">Adventure</a>,
					<a
						href=""/search/title?genres=drama&explore=title_type,genres&ref_=tt_ov_inf"">Drama</a>
					<span class=""ghost"">|</span>
					<a href=""/title/tt0944947/releaseinfo?ref_=tt_ov_inf""
						title=""See more release dates"">TV Series (2011�2019)
					</a> </div>
			</div>
		</<html>";

		private const string SeasonAirDatesForMay =
		@"
		<div class=""info"" itemprop=""episodes"" >
			<div class=""airdate"">
				8 May 2020
			</div>
		</div>";

		private const string PastSeasonAirDates =
		@"
		<div class=""info"" itemprop=""episodes"" >
			<div class=""airdate"">
				8 Mar. 2020
			</div>
			<div class=""airdate"">
				8 Dec. 2020
			</div>
			<div class=""airdate"">
				8 Oct. 2020
			</div>
		</div>";

		private const string FutureSeasonAirDates =
		@"
		<div class=""info"" itemprop=""episodes"" >
			<div class=""airdate"">
				8 Mar. 2100
			</div>
			<div class=""airdate"">
				21 Dec. 2100
			</div>
			<div class=""airdate"">
				9 Oct. 2100
			</div
			<div class=""airdate"">
				31 Jan. 2100
			</div>
		</div>";

	}
}
