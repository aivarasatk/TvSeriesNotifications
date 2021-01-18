using System.Linq;
using TVSeriesNotifications.BusinessLogic;
using TVSeriesNotifications.CustomExceptions;
using Xunit;

namespace TVSeriesNotifications.Tests
{
    public class HtmlParserTests
    {
        [Fact]
        public void GivenValidHtml_SeasonNodesAreReturned()
        {
			// Arrange & Act
			var result = HtmlParser.SeasonNodes(ValidHtmlWith8SeasonNodes);

			Assert.NotEmpty(result);
        }

		[Fact]
		public void GivenValidHtml_SeasonNodesHaveCorrectSeasonValues()
		{
			// Arrange 
			var actualSeasons = new[] { "1", "2", "3", "4", "5", "6", "7", "8" };

			// Act
			var result = HtmlParser.SeasonNodes(ValidHtmlWith8SeasonNodes).ToList();

			Assert.True(result.Count == 8);

			foreach (var season in actualSeasons)
				Assert.Contains(result, r => r.InnerText == season);
		}

		[Fact]
		public void GivenValidHtml_SeasonNodesHaveLinks()
		{
			// Arrange & Act
			var result = HtmlParser.SeasonNodes(ValidHtmlWith8SeasonNodes).ToList();

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
			Assert.Throws<ImdbHtmlChangedException>(() => HtmlParser.SeasonNodes("<html></html>"));
		}

		[Fact]
		public void GivenHtmlWithoutLinksToSeason_SeasonNodesThrows()
		{
			// Arrange & Act & Assert
			Assert.Throws<ImdbHtmlChangedException>(() => HtmlParser.SeasonNodes("<html><div class=\"seasons-and-year-nav\"></div></html>"));
		}

		private string ValidHtmlWith8SeasonNodes =
		@"<html>
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
		</html>";

	}
}
