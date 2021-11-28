namespace TVSeriesNotifications.Domain.Ports.Repository
{
    public interface ITvShowRepository
    {
        Task<IEnumerable<string>> RetrieveTvShows();
    }
}
