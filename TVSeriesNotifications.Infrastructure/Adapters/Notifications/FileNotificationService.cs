using TVSeriesNotifications.Domain.Models;
using TVSeriesNotifications.Domain.Ports.Notifications;

namespace TVSeriesNotifications.Notifications
{
    public class FileNotificationService : INotificationService
    {
        private const string ErrorFileName = "TvShowCheckErrors.txt";
        private const string NewSeasonFileName = "NewTvShowSeason.txt";
        private readonly string _fileLocation;

        public FileNotificationService()
        {
            _fileLocation = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        public void NotifyAboutErrors(string message)
        {
            lock (ErrorFileName)
            {
                File.AppendAllText(Path.Combine(_fileLocation, ErrorFileName), message);
            }
        }

        public void NotifyNewSeason(NewSeason season)
        {
            if (season is null)
                throw new ArgumentNullException(nameof(season));

            lock (NewSeasonFileName)
            {
                File.AppendAllText(Path.Combine(_fileLocation, NewSeasonFileName), $"{season.TvShow} season {season.Season} has aired");
            }
        }
    }
}
