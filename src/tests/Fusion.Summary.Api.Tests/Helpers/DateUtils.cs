using System.Diagnostics;

namespace Fusion.Summary.Api.Tests.Helpers;

public static class DateUtils
{
    public static DateTime GetPreviousWeeksMonday(DateTime date)
    {
        switch (date.DayOfWeek)
        {
            case DayOfWeek.Sunday:
                return date.AddDays(-6);
            case DayOfWeek.Monday:
                return date.AddDays(-7);
            case DayOfWeek.Tuesday:
            case DayOfWeek.Wednesday:
            case DayOfWeek.Thursday:
            case DayOfWeek.Friday:
            case DayOfWeek.Saturday:
            default:
            {
                // Calculate days until previous monday
                // Go one week back and then remove the days until the monday
                var daysUntilLastWeeksMonday = 1 - (int)date.DayOfWeek - 7;

                return date.AddDays(daysUntilLastWeeksMonday);
            }
        }
    }
}