using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TVSeriesNotifications.JsonModels;

namespace TVSeriesNotifications.Api
{
    public class ImdbClient : IImdbClient, IDisposable
    {
        private const string BaseSuggestionUrl = "https://v2.sg.media-imdb.com/suggestion";
        private const string BaseImdbUrl = "https://www.imdb.com";
        private const string BaseTitleSearch = "https://www.imdb.com/title";

        private readonly HttpClient _httpClient;

        public ImdbClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> GetPageContentsAsync(string tvShowId)
        {
            return await GetRequestAsync(Path.Combine(BaseTitleSearch, tvShowId) + "/");
        }

        public async Task<string> GetSeasonPageContentsAsync(string tvShowId, int season)
        {
            var link = BuildTvShowSeasonLink(tvShowId, season);
            return await GetRequestAsync($"{BaseImdbUrl}/{link}"); // Using Path.Combine provides a link that gives
        }

        private static string BuildTvShowSeasonLink(string tvShowId, int season)
            => $"title/{tvShowId}/episodes?season={season}&ref_=tt_eps_sn_{season}";

        public async Task<ImdbSuggestion> GetSuggestionsAsync(string searchValue)
        {
            var urlReadySearchValue = $"{searchValue.Replace(' ', '_')}.json";
            var index = searchValue.Substring(0, 1).ToLower();

            return await GetRequestAsync<ImdbSuggestion>($"{BaseSuggestionUrl}/{index}/{urlReadySearchValue}");
        }

        private async Task<string> GetRequestAsync(string url)
        {
            using var response = await _httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<T> GetRequestAsync<T>(string url)
        {
            var content = await GetRequestAsync(url);
            return JsonConvert.DeserializeObject<T>(content);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
