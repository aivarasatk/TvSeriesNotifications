using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using TVSeriesNotifications.Domain.Models;

namespace TVSeriesNotifications.Infrastructure.Adapters.ImdbClient.Domain
{
    [DataContract]
    public class Suggestion
    {
        public Suggestion(string category, string id, string title, int yearStart, string yearRange)
        {
            _category = category;
            Id = id;
            Title = title;
            YearStart = yearStart;
            YearRange = yearRange;
        }

        [Required]
        [DataMember(Name = "id")]
        public string Id { get; init; }

        [Required]
        [DataMember(Name = "l")]
        public string Title { get; init; }

        [Required]
        [DataMember(Name = "q")]
        private string _category;

        public TVCategory Category
        {
            get
            {
                return _category switch
                {
                    "TV series" => TVCategory.TVSeries,
                    "TV mini-series" => TVCategory.TVMiniSeries,
                    _ => TVCategory.Undefined
                };
            }
        }

        [Required]
        [DataMember(Name = "y")]
        public int YearStart { get; init; }

        [Required]
        [DataMember(Name = "yr")]
        public string YearRange { get; init; }
    }
}
