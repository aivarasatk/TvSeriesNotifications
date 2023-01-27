using System.Runtime.Serialization;

namespace TVSeriesNotifications.Infrastructure.Adapters.ImdbClient.Domain
{
    [DataContract]
    public class ImdbSuggestion
    {
        [DataMember(Name = "d")]
        public IEnumerable<Suggestion>? Suggestions { get; init; }
    }
}
