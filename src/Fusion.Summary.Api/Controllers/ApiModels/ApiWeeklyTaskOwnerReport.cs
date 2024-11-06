using Fusion.Summary.Api.Domain.Models;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiWeeklyTaskOwnerReport
{
    public required Guid Id { get; set; }
    public required Guid ProjectId { get; set; }
    public required DateTime PeriodStart { get; set; }
    public required DateTime PeriodEnd { get; set; }

    public required int ActionsAwaitingTaskOwnerAction { get; set; }
    public required ApiAdminAccessExpiring[] AdminAccessExpiringInLessThanThreeMonths { get; set; }
    public required ApiPositionAllocationEnding[] PositionAllocationsEndingInNextThreeMonths { get; set; }
    public required ApiTBNPositionStartingSoon[] TBNPositionsStartingInLessThanThreeMonths { get; set; }


    public static ApiWeeklyTaskOwnerReport FromQueryWeeklyTaskOwnerReport(QueryWeeklyTaskOwnerReport queryReport)
    {
        return new ApiWeeklyTaskOwnerReport
        {
            Id = queryReport.Id,
            ProjectId = queryReport.ProjectId,
            PeriodStart = queryReport.Period.Start,
            PeriodEnd = queryReport.Period.End,
            ActionsAwaitingTaskOwnerAction = queryReport.ActionsAwaitingTaskOwnerAction,
            AdminAccessExpiringInLessThanThreeMonths = queryReport.AdminAccessExpiringInLessThanThreeMonths.Select(x =>
                new ApiAdminAccessExpiring()
                {
                    AzureUniqueId = x.AzureUniqueId,
                    FullName = x.FullName,
                    Expires = x.Expires
                }).ToArray(),
            PositionAllocationsEndingInNextThreeMonths = queryReport.PositionAllocationsEndingInNextThreeMonths.Select(x =>
                new ApiPositionAllocationEnding()
                {
                    PositionName = x.PositionName,
                    PositionNameDetailed = x.PositionNameDetailed,
                    PositionAppliesTo = x.PositionAppliesTo,
                    PositionExternalId = x.PositionExternalId
                }).ToArray(),
            TBNPositionsStartingInLessThanThreeMonths = queryReport.TBNPositionsStartingInLessThanThreeMonths.Select(x =>
                new ApiTBNPositionStartingSoon()
                {
                    PositionName = x.PositionName,
                    PositionNameDetailed = x.PositionNameDetailed,
                    PositionAppliesFrom = x.PositionAppliesFrom,
                    PositionExternalId = x.PositionExternalId
                }).ToArray()
        };
    }
}

public class ApiAdminAccessExpiring
{
    public required Guid AzureUniqueId { get; set; }
    public required string FullName { get; set; }
    public required DateTime Expires { get; set; }
}

public class ApiPositionAllocationEnding
{
    public required string PositionExternalId { get; set; }

    public required string PositionName { get; set; }

    public required string PositionNameDetailed { get; set; }

    public required DateTime PositionAppliesTo { get; set; }
}

public class ApiTBNPositionStartingSoon
{
    public required string PositionExternalId { get; set; }
    public required string PositionName { get; set; }
    public required string PositionNameDetailed { get; set; }
    public required DateTime PositionAppliesFrom { get; set; }
}