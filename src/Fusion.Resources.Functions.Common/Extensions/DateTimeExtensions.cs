namespace Fusion.Resources.Functions.Common.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    ///     Returns a new DateTime object with the date set to monday last week.
    /// </summary>
    public static DateTime GetPreviousWeeksMondayDate(this DateTime date)
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
                var daysUntilLastWeeksMonday = (1 - (int)date.DayOfWeek) - 7;

                return date.AddDays(daysUntilLastWeeksMonday);
            }
        }
    }
}