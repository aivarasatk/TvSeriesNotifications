using System;
using System.Linq;
using TVSeriesNotifications.Domain.Models;

public static class SeasonHelper
{
    public static bool HasUpcomingSeason(int firstUpcomingSeason) => firstUpcomingSeason != default;

    public static bool IsUpcomingSeason(int currentSeason, int latestAiredSeason) // there can be more than one confirmed seasons
        => currentSeason > latestAiredSeason;

    public static bool TvShowToBeAired(int season, int seasonCount) => season is 1 && seasonCount is 1;

    public static bool ShowIsOnGoing(TvShow tvShowSuggestion, DateTime today)
    {
        // Ongoing examples Year: 2018, 2100 (upcoming)
        // Ongoing examples Year: 2018 or YearRange: 2018-2100 (future year)
        // Ended examples YearRange: 2019-2019, 2015-2019
        // Ongoing Mini series YearRange: 2100-2100 (current/future year)

        if (tvShowSuggestion.YearRange is null)
            return true;

        var yearRangeSplit = tvShowSuggestion.YearRange
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(y => int.Parse(y))
            .ToArray();

        return OngoingSeries(tvShowSuggestion, yearRangeSplit, today) 
            || OngoingMiniSeries(tvShowSuggestion, yearRangeSplit, today);
    }

    private static bool OngoingMiniSeries(TvShow tvShowSuggestion, int[] yearRangeSplit, DateTime today)
        => tvShowSuggestion.Category is TVCategory.TVMiniSeries && yearRangeSplit[1] >= today.Year;

    private static bool OngoingSeries(TvShow tvShowSuggestion, int[] yearRangeSplit, DateTime today)
        => tvShowSuggestion.Category is TVCategory.TVSeries
        && yearRangeSplit[1] >= today.Year;
}