using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVSeriesNotifications.JsonModels;

namespace TVSeriesNotifications.Api
{
    public interface IImdbClient
    {
        Task<string> GetPageContentsAsync(string tvShowId);

        Task<ImdbSuggestion> GetSuggestionsAsync(string searchValue);

        Task<string> GetSeasonPageContentsAsync(string link);
    }
}
