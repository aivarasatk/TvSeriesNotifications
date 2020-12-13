using System;
using System.IO;
using System.Threading.Tasks;

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

        public Task NotifyAboutErrors(string message)
        {
            lock (ErrorFileName)
            {
                return File.AppendAllTextAsync(Path.Combine(_fileLocation, ErrorFileName), message);
            }
        }

        public Task NotifyAboutNewSeason(string tvShow, int newSeason)
        {
            lock (NewSeasonFileName)
            {
                return File.AppendAllTextAsync(Path.Combine(_fileLocation, NewSeasonFileName), $"{tvShow} season {newSeason} has aired");
            }
        }
    }
}
