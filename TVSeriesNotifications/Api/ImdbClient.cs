using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TVSeriesNotifications.JsonModels;

namespace TVSeriesNotifications.Api
{
    public class ImdbClient : IImdbClient
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

        public async Task<string> GetSeasonPageContentsAsync(string link)
        {
            return await GetRequestAsync($"{BaseImdbUrl}{link}");
        }

        public async Task<ImdbSuggestion> GetSuggestionsAsync(string searchValue)
        {
            var urlReadySearchValue = $"{searchValue.Replace(' ', '_')}.json";
            var index = searchValue.Substring(0, 1).ToLower();

            return await GetRequestAsync<ImdbSuggestion>(Path.Combine(BaseSuggestionUrl, index, urlReadySearchValue));
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
    }
}
