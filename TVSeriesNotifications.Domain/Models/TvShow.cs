namespace TVSeriesNotifications.Domain.Models
{
    public record TvShow(
        string Id,
        string Title,
        TVCategory Category,
        int YearStart,
        string YearRange);
}