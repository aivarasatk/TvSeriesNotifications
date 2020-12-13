using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace TVSeriesNotifications.Persistance
{
    public class FileTvShowRepository : ITvShowRepository
    {
        private const string CsvLocation = "TvShowList.csv";
        private readonly IFileSystem _fileSystem;

        public FileTvShowRepository(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task<IEnumerable<string>> RetrieveTvShows()
        {
            var fileContents = await _fileSystem.File.ReadAllTextAsync(CsvLocation);
            return fileContents.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        }
    }
}
