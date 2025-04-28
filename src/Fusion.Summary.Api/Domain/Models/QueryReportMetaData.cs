namespace Fusion.Summary.Api.Domain.Models;

public class QueryReportMetaData
{
    public QueryReportMetaData(Guid id, DateTime period)
    {
        Id = id;
        Period = period;
    }

    public Guid Id { get; init; }
    public DateTime Period { get; init; }
}