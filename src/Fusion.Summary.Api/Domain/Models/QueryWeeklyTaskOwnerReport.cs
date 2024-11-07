using Fusion.Summary.Api.Database.Models;

namespace Fusion.Summary.Api.Domain.Models;

public class QueryWeeklyTaskOwnerReport
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required Period Period { get; set; }

    public required int ActionsAwaitingTaskOwnerAction { get; set; }
    public required List<AdminAccessExpiring> AdminAccessExpiringInLessThanThreeMonths { get; set; }
    public required List<PositionAllocationEnding> PositionAllocationsEndingInNextThreeMonths { get; set; }
    public required List<TBNPositionStartingSoon> TBNPositionsStartingInLessThanThreeMonths { get; set; }


    public static DbWeeklyTaskOwnerReport ToDbWeeklyTaskOwnerReport(QueryWeeklyTaskOwnerReport queryReport)
    {
        return new DbWeeklyTaskOwnerReport
        {
            Id = queryReport.Id,
            ProjectId = queryReport.ProjectId,
            PeriodStart = queryReport.Period.Start,
            PeriodEnd = queryReport.Period.End,
            ActionsAwaitingTaskOwnerAction = queryReport.ActionsAwaitingTaskOwnerAction,
            AdminAccessExpiringInLessThanThreeMonths = queryReport.AdminAccessExpiringInLessThanThreeMonths.Select(x =>
                new DbAdminAccessExpiring()
                {
                    AzureUniqueId = x.AzureUniqueId,
                    FullName = x.FullName,
                    Expires = x.Expires
                }).ToList(),
            PositionAllocationsEndingInNextThreeMonths = queryReport.PositionAllocationsEndingInNextThreeMonths.Select(x =>
                new DbPositionAllocationEnding()
                {
                    PositionName = x.PositionName,
                    PositionNameDetailed = x.PositionNameDetailed,
                    PositionAppliesTo = x.PositionAppliesTo,
                    PositionExternalId = x.PositionExternalId
                }).ToList(),
            TBNPositionsStartingInLessThanThreeMonths = queryReport.TBNPositionsStartingInLessThanThreeMonths.Select(x =>
                new DbTBNPositionStartingSoon()
                {
                    PositionName = x.PositionName,
                    PositionNameDetailed = x.PositionNameDetailed,
                    PositionAppliesFrom = x.PositionAppliesFrom,
                    PositionExternalId = x.PositionExternalId
                }).ToList()
        };
    }

    public static QueryWeeklyTaskOwnerReport FromDbWeeklyTaskOwnerReport(DbWeeklyTaskOwnerReport dbReport)
    {
        return new QueryWeeklyTaskOwnerReport
        {
            Id = dbReport.Id,
            ProjectId = dbReport.ProjectId,
            Period = new Period(Period.PeriodType.Weekly, dbReport.PeriodStart, dbReport.PeriodEnd),
            ActionsAwaitingTaskOwnerAction = dbReport.ActionsAwaitingTaskOwnerAction,
            AdminAccessExpiringInLessThanThreeMonths = dbReport.AdminAccessExpiringInLessThanThreeMonths.Select(x =>
                new AdminAccessExpiring()
                {
                    AzureUniqueId = x.AzureUniqueId,
                    FullName = x.FullName,
                    Expires = x.Expires
                }).ToList(),
            PositionAllocationsEndingInNextThreeMonths = dbReport.PositionAllocationsEndingInNextThreeMonths.Select(x =>
                new PositionAllocationEnding()
                {
                    PositionName = x.PositionName,
                    PositionNameDetailed = x.PositionNameDetailed,
                    PositionAppliesTo = x.PositionAppliesTo,
                    PositionExternalId = x.PositionExternalId
                }).ToList(),
            TBNPositionsStartingInLessThanThreeMonths = dbReport.TBNPositionsStartingInLessThanThreeMonths.Select(x =>
                new TBNPositionStartingSoon()
                {
                    PositionName = x.PositionName,
                    PositionNameDetailed = x.PositionNameDetailed,
                    PositionAppliesFrom = x.PositionAppliesFrom,
                    PositionExternalId = x.PositionExternalId
                }).ToList()
        };
    }
}

public class AdminAccessExpiring
{
    public required Guid AzureUniqueId { get; set; }
    public required string FullName { get; set; }
    public required DateTime Expires { get; set; }
}

public class PositionAllocationEnding
{
    public required string PositionExternalId { get; set; }

    public required string PositionName { get; set; }

    public required string PositionNameDetailed { get; set; }

    public required DateTime PositionAppliesTo { get; set; }
}

public class TBNPositionStartingSoon
{
    public required string PositionExternalId { get; set; }
    public required string PositionName { get; set; }
    public required string PositionNameDetailed { get; set; }
    public required DateTime PositionAppliesFrom { get; set; }
}