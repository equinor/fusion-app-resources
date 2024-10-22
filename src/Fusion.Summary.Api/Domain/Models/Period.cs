namespace Fusion.Summary.Api.Domain.Models;

public sealed class Period
{
    public PeriodType Type { get; init; }
    public DateTime Start { get; init; }
    public DateTime End { get; init; }

    public Period(PeriodType type, DateTime start, DateTime end)
    {
        switch (type)
        {
            case PeriodType.Weekly:
                if (end - start != TimeSpan.FromDays(7))
                    throw new ArgumentException("Weekly report period must be exactly 7 days", nameof(end));
                if (start.DayOfWeek != DayOfWeek.Monday)
                    throw new ArgumentException("Weekly report period must start on a Monday", nameof(start));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        Type = type;
        Start = start.Date;
        End = end.Date;
    }

    public static Period FromStart(PeriodType type, DateTime start)
    {
        var end = type switch
        {
            PeriodType.Weekly => start.AddDays(7),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        return new Period(type, start, end);
    }


    public static Period FromEnd(PeriodType type, DateTime end)
    {
        var start = type switch
        {
            PeriodType.Weekly => end.AddDays(-7),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        return new Period(type, start, end);
    }
}