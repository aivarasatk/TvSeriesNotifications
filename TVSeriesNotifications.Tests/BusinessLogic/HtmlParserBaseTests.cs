using System;
using TVSeriesNotifications.Infrastructure.Adapters.HtmlParser;
using Xunit;

namespace TVSeriesNotifications.Tests.BusinessLogic
{
    public class HtmlParserBaseTests
    {
        [Fact]
        public void GivenHTLMContainingSearchKeyword_ResolveHtmlElement_ShouldSuccessfullyReturn()
        {
            var element = HtmlParserBase.TryResolveHtmlElement(PartialHTMLContainingOutlanderAirDate, "Sat, Aug 9, 2014");
            Assert.Equal(element, new HtmlElement("span", "class", "sc-90c501a1-10 hjpnXD"));
        }

        [Theory]
        [InlineData(@"<span class=""sc-90c501a1-10 hjpnXD"">Sat, Aug 9, 2014</span>", "Sat, Aug 9, 2014", "span", "class", "sc-90c501a1-10 hjpnXD")]
        [InlineData(@"<div class=""hjpnXD"">Sat, Aug 9</div>", "Sat, Aug 9", "div", "class", "hjpnXD")]
        [InlineData(@"<a class=""somie classie"" other=""empty"">KEYWORD</a>", "KEYWORD", "a", "class", "somie classie")]
        public void ResolveHtmlElement_ShouldParseVariousTags(string html,string keyword, string tag, string attribute, string classValue)
        {
            var result = HtmlParserBase.TryResolveHtmlElement(html, keyword);
            Assert.Equal(result, new HtmlElement(tag, attribute, classValue));
        }

        [Theory]
        [InlineData(@"<span id=""sc-90c501a1-10 hjpnXD"">Sat, Aug 9, 2014</span>")]
        [InlineData(@"<div title=""hjpnXD"">Sat, Aug 9</div>")]
        [InlineData(@"<a li=""somie classie"" other=""empty"">KEYWORD</a>")]
        public void ResolveHtmlElement_ShouldThrowWhenClassAttributeNotFound(string html)
        {
            Assert.Throws<Exception>(() => HtmlParserBase.TryResolveHtmlElement(html, "anything"));
        }

        [Theory]
        [InlineData(@"<spanu class=""sc-90c501a1-10 hjpnXD"">Sat, Aug 9, 2014</span>")]
        [InlineData(@"<divu class=""hjpnXD"">Sat, Aug 9</div>")]
        [InlineData(@"<ali class=""somie classie"" other=""empty"">KEYWORD</a>")]
        public void ResolveHtmlElement_ShouldThrowOnIllegalTag(string html)
        {
            Assert.Throws<Exception>(() => HtmlParserBase.TryResolveHtmlElement(html, "anything"));
        }


        private const string PartialHTMLContainingOutlanderAirDate =
        @"
            <div class=""sc-90c501a1-1 QGAUa""><div data-testid=""watchlist-ribbon-add"" class=""ipc-watchlist-ribbon ipc-focusable ipc-watchlist-ribbon--m ipc-watchlist-ribbon--base ipc-watchlist-ribbon--onImage sc-90c501a1-3 XHlHg"" aria-label=""Add to Watchlist"" role=""button"" tabindex=""0""><svg class=""ipc-watchlist-ribbon__bg"" width=""24px"" height=""34px"" viewBox=""0 0 24 34"" xmlns=""http://www.w3.org/2000/svg"" role=""presentation""><polygon class=""ipc-watchlist-ribbon__bg-ribbon"" fill=""#000000"" points=""24 0 0 0 0 32 12.2436611 26.2926049 24 31.7728343""></polygon><polygon class=""ipc-watchlist-ribbon__bg-hover"" points=""24 0 0 0 0 32 12.2436611 26.2926049 24 31.7728343""></polygon><polygon class=""ipc-watchlist-ribbon__bg-shadow"" points=""24 31.7728343 24 33.7728343 12.2436611 28.2926049 0 34 0 32 12.2436611 26.2926049""></polygon></svg><div class=""ipc-watchlist-ribbon__icon"" role=""presentation""><svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" class=""ipc-icon ipc-icon--add ipc-icon--inline"" viewBox=""0 0 24 24"" fill=""currentColor"" role=""presentation""><path d=""M18 13h-5v5c0 .55-.45 1-1 1s-1-.45-1-1v-5H6c-.55 0-1-.45-1-1s.45-1 1-1h5V6c0-.55.45-1 1-1s1 .45 1 1v5h5c.55 0 1 .45 1 1s-.45 1-1 1z""></path></svg></div></div><div class=""ipc-slate ipc-slate--base ipc-slate--dynamic-width sc-90c501a1-2 eWbIvC ipc-sub-grid-item ipc-sub-grid-item--span-4"" role=""group""><div class=""ipc-media ipc-media--slate-16x9 ipc-image-media-ratio--slate-16x9 ipc-media--media-radius ipc-media--base ipc-media--slate-m ipc-slate__slate-image ipc-media__img"" style=""width:100%""><img alt=""Caitríona Balfe, Sam Heughan, and Grant O'Rourke in Outlander (2014)"" class=""ipc-image"" loading=""lazy"" src=""https://m.media-amazon.com/images/M/MV5BMTQxMDA2MTAzMF5BMl5BanBnXkFtZTgwODY3MzkyMjE@._V1_QL75_UX500_CR0,26,500,281_.jpg"" srcset=""https://m.media-amazon.com/images/M/MV5BMTQxMDA2MTAzMF5BMl5BanBnXkFtZTgwODY3MzkyMjE@._V1_QL75_UX500_CR0,26,500,281_.jpg 500w, https://m.media-amazon.com/images/M/MV5BMTQxMDA2MTAzMF5BMl5BanBnXkFtZTgwODY3MzkyMjE@._V1_QL75_UX750_CR0,39,750,422_.jpg 750w, https://m.media-amazon.com/images/M/MV5BMTQxMDA2MTAzMF5BMl5BanBnXkFtZTgwODY3MzkyMjE@._V1_QL75_UX1000_CR0,52,1000,563_.jpg 1000w"" sizes=""100vw, (min-width: 480px) 68vw, (min-width: 600px) 52vw, (min-width: 1024px) 32vw, (min-width: 1280px) 32vw"" width=""500""></div><a class=""ipc-lockup-overlay ipc-focusable"" href=""/title/tt3244568/?ref_=ttep_ep_1"" aria-label=""Sassenach""><div class=""ipc-lockup-overlay__screen""></div></a></div><div class=""sc-90c501a1-4 ihHLol""><div class=""sc-90c501a1-5 fTuoTs""><h4 data-testid=""slate-list-card-title"" class=""sc-90c501a1-7 kczCGp""><div class=""ipc-title ipc-title--base ipc-title--title ipc-title--title--reduced ipc-title-link-no-icon ipc-title--on-textPrimary sc-90c501a1-8 hOQFSg""><a href=""/title/tt3244568/?ref_=ttep_ep_1"" class=""ipc-title-link-wrapper"" tabindex=""0""><div class=""ipc-title__text ipc-title__text--reduced"">S1.E1 ∙ Sassenach</div></a></div></h4><span class=""sc-90c501a1-10 hjpnXD"">Sat, Aug 9, 2014</span></div><div class=""sc-90c501a1-11 ldHJVZ""><div class=""ipc-overflowText ipc-overflowText--base"" style=""max-height: 72px;""><div class=""ipc-overflowText--children""><div class=""ipc-html-content ipc-html-content--base ipc-html-content--display-inline"" role=""presentation""><div class=""ipc-html-content-inner-div"" role=""presentation"">1945 England: Claire Randall reunites with her husband after five years of war. A second honeymoon goes awry when she falls back through time to 1740's Scotland.</div></div></div></div></div><div class=""sc-90c501a1-12 cYWcIf""><div class=""sc-98721807-0 kVYFTC sc-b71e81f5-3 CDmWM"" data-testid=""ratingGroup--container""><span aria-label=""IMDb rating: 8.3"" class=""ipc-rating-star ipc-rating-star--base ipc-rating-star--imdb ratingGroup--imdb-rating"" data-testid=""ratingGroup--imdb-rating""><svg width=""24"" height=""24"" xmlns=""http://www.w3.org/2000/svg"" class=""ipc-icon ipc-icon--star-inline"" viewBox=""0 0 24 24"" fill=""currentColor"" role=""presentation""><path d=""M12 20.1l5.82 3.682c1.066.675 2.37-.322 2.09-1.584l-1.543-6.926 5.146-4.667c.94-.85.435-2.465-.799-2.567l-6.773-.602L13.29.89a1.38 1.38 0 0 0-2.581 0l-2.65 6.53-6.774.602C.052 8.126-.453 9.74.486 10.59l5.147 4.666-1.542 6.926c-.28 1.262 1.023 2.26 2.09 1.585L12 20.099z""></path></svg><span class=""ipc-rating-star--rating"">8.3</span><span class=""ipc-rating-star--maxRating"">/<!-- -->10</span><span class=""ipc-rating-star--voteCount"">&nbsp;(<!-- -->7.9K<!-- -->)</span></span><button aria-label=""Rate Sassenach"" class=""ipc-rate-button sc-98721807-1 ipNYdR ratingGroup--user-rating ipc-rate-button--unrated ipc-rate-button--base"" data-testid=""rate-button""><span class=""ipc-rating-star ipc-rating-star--base ipc-rating-star--rate""><svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" class=""ipc-icon ipc-icon--star-border-inline"" viewBox=""0 0 24 24"" fill=""currentColor"" role=""presentation""><path d=""M22.724 8.217l-6.786-.587-2.65-6.22c-.477-1.133-2.103-1.133-2.58 0l-2.65 6.234-6.772.573c-1.234.098-1.739 1.636-.8 2.446l5.146 4.446-1.542 6.598c-.28 1.202 1.023 2.153 2.09 1.51l5.818-3.495 5.819 3.509c1.065.643 2.37-.308 2.089-1.51l-1.542-6.612 5.145-4.446c.94-.81.45-2.348-.785-2.446zm-10.726 8.89l-5.272 3.174 1.402-5.983-4.655-4.026 6.141-.531 2.384-5.634 2.398 5.648 6.14.531-4.654 4.026 1.402 5.983-5.286-3.187z""></path></svg><span class=""ipc-rating-star--rate"">Rate</span></span></button></div></div></div></div>
        ";
    }
}
