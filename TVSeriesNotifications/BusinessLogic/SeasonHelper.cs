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
        // Ended examples 2019, 2015-2019
        // Ongoing examples 2018-, 2100- (upcoming), 2018-2100(ends in current year + n years)
        // Mini series 2100 (upcoming), Mini series for current year
        var yearRangeSplit = tvShowSuggestion.YearRange
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(y => int.Parse(y))
            .ToArray();

        return OngoingSeries(tvShowSuggestion, yearRangeSplit, today) 
            || OngoingMiniSeries(tvShowSuggestion, yearRangeSplit, today);
    }

    private static bool OngoingMiniSeries(TvShow tvShowSuggestion, int[] yearRangeSplit, DateTime today)
        => tvShowSuggestion.Category is TVCategory.TVMiniSeries && yearRangeSplit[0] >= today.Year;

    private static bool OngoingSeries(TvShow tvShowSuggestion, int[] yearRangeSplit, DateTime today)
        => tvShowSuggestion.Category is not TVCategory.TVMiniSeries
        && (tvShowSuggestion.YearRange.Last() is '-' || (yearRangeSplit.Length == 2 && yearRangeSplit[1] >= today.Year));
}