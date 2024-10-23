using Fusion.Summary.Api.Database.Models;

namespace Fusion.Summary.Api.Domain.Models;

public class QueryWeeklyTaskOwnerReport
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required Period Period { get; set; }


    public static DbWeeklyTaskOwnerReport ToDbWeeklyTaskOwnerReport(QueryWeeklyTaskOwnerReport queryWeeklyTaskOwnerReport)
    {
        return new DbWeeklyTaskOwnerReport
        {
            Id = queryWeeklyTaskOwnerReport.Id,
            ProjectId = queryWeeklyTaskOwnerReport.ProjectId,
            PeriodStart = queryWeeklyTaskOwnerReport.Period.Start,
            PeriodEnd = queryWeeklyTaskOwnerReport.Period.End
        };
    }

    public static QueryWeeklyTaskOwnerReport FromDbWeeklyTaskOwnerReport(DbWeeklyTaskOwnerReport dbWeeklyTaskOwnerReport)
    {
        return new QueryWeeklyTaskOwnerReport
        {
            Id = dbWeeklyTaskOwnerReport.Id,
            ProjectId = dbWeeklyTaskOwnerReport.ProjectId,
            Period = new Period(Period.PeriodType.Weekly, dbWeeklyTaskOwnerReport.PeriodStart, dbWeeklyTaskOwnerReport.PeriodEnd)
        };
    }
}