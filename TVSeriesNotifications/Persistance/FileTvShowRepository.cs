using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TVSeriesNotifications.Persistance
{
    public class FileTvShowRepository : ITvShowRepository
    {
        private const string CsvLocation = "TvShowList.csv";

        public async Task<IEnumerable<string>> RetrieveTvShows()
        {
            var fileContents = await File.ReadAllTextAsync(CsvLocation);
            return fileContents.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        }
    }
}
