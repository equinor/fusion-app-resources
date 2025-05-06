namespace Fusion.Summary.Api.Domain.Models;

public class QueryReportMetaData
{
    public QueryReportMetaData(Guid id, DateTime period)
    {
        Id = id;
        Period = period;
        PeriodEnd = period.AddDays(7);
    }

    public Guid Id { get; init; }
    public DateTime Period { get; init; }
    public DateTime PeriodEnd { get; init; }
}