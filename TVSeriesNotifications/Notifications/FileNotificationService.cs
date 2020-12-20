using System;
using System.IO;
using System.Threading.Tasks;
using TVSeriesNotifications.DTO;

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

        public FileNotificationService(string path)
        {
            Directory.CreateDirectory(path);
        }

        public Task NotifyAboutErrorsAsync(string message)
        {
            lock (ErrorFileName)
            {
                return File.AppendAllTextAsync(Path.Combine(_fileLocation, ErrorFileName), message);
            }
        }

        public Task NotifyNewSeasonAsync(NewSeason season)
        {
            if (season is null)
                throw new ArgumentNullException(nameof(season));

            lock (NewSeasonFileName)
            {
                return File.AppendAllTextAsync(Path.Combine(_fileLocation, NewSeasonFileName), $"{season.TvShow} season {season.Season} has aired");
            }
        }
    }
}
