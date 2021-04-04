using System.Collections.Generic;

namespace TVSeriesNotifications.DTO
{
    public record SeasonNode(string InnerText, IEnumerable<HtmlAttribute> Attributes);

    public record HtmlAttribute(string Name, string Value);
}
