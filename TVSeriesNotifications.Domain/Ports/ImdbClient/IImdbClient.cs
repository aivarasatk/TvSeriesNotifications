using TVSeriesNotifications.Domain.Models;

namespace TVSeriesNotifications.Domain.Ports.ImdbClient
{
    public interface IImdbClient
    {
        Task<string> GetPageContentsAsync(string tvShowId);

        Task<IEnumerable<TvShow>> GetSuggestionsAsync(string searchValue);

        Task<string> GetSeasonPageContentsAsync(string tvShowId, int season);
    }
}
