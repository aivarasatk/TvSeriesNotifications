using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TVSeriesNotifications.JsonModels
{
    [DataContract]
    public class ImdbSuggestion
    {
        [DataMember(Name = "d")]
        public IEnumerable<Suggestion> Suggestions { get; init; }
    }
}
