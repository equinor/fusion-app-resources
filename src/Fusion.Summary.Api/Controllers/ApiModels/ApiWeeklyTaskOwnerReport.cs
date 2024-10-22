using Fusion.Summary.Api.Domain.Models;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiWeeklyTaskOwnerReport
{
    public required Guid Id { get; set; }
    public required Guid ProjectId { get; set; }
    public required DateTime PeriodStart { get; set; }
    public required DateTime PeriodEnd { get; set; }


    public static ApiWeeklyTaskOwnerReport FromQueryWeeklyTaskOwnerReport(QueryWeeklyTaskOwnerReport queryWeeklyTaskOwnerReport)
    {
        return new ApiWeeklyTaskOwnerReport
        {
            Id = queryWeeklyTaskOwnerReport.Id,
            ProjectId = queryWeeklyTaskOwnerReport.ProjectId,
            PeriodStart = queryWeeklyTaskOwnerReport.Period.Start,
            PeriodEnd = queryWeeklyTaskOwnerReport.Period.End
        };
    }
}