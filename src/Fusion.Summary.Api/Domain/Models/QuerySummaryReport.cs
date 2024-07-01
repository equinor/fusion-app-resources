using Fusion.Summary.Api.Database.Models;

namespace Fusion.Summary.Api.Domain.Models;

public class QuerySummaryReport
{
    public required Guid Id { get; set; }
    public required string DepartmentSapId { get; set; }
    public required SummaryReportPeriod PeriodType { get; set; }
    public required DateTime Period { get; set; }
    public required string NumberOfPersonnel { get; set; }
    public required string CapacityInUse { get; set; }
    public required string NumberOfRequestsLastPeriod { get; set; }
    public required string NumberOfOpenRequests { get; set; }
    public required string NumberOfRequestsStartingInLessThanThreeMonths { get; set; }
    public required string NumberOfRequestsStartingInMoreThanThreeMonths { get; set; }
    public required string AverageTimeToHandleRequests { get; set; }
    public required string AllocationChangesAwaitingTaskOwnerAction { get; set; }
    public required string ProjectChangesAffectingNextThreeMonths { get; set; }

    public required EndingPosition[] PositionsEnding { get; set; }
    public required PersonnelMoreThan100PercentFTE[] PersonnelMoreThan100PercentFTE { get; set; }


    public static QuerySummaryReport FromDbSummaryReport(DbSummaryReport dbSummaryReport)
    {
        return new QuerySummaryReport
        {
            Id = dbSummaryReport.Id,
            DepartmentSapId = dbSummaryReport.DepartmentSapId,
            PeriodType = Enum.Parse<SummaryReportPeriod>(dbSummaryReport.PeriodType.ToString()),
            Period = dbSummaryReport.Period,
            NumberOfPersonnel = dbSummaryReport.NumberOfPersonnel,
            CapacityInUse = dbSummaryReport.CapacityInUse,
            NumberOfRequestsLastPeriod = dbSummaryReport.NumberOfRequestsLastPeriod,
            NumberOfOpenRequests = dbSummaryReport.NumberOfOpenRequests,
            NumberOfRequestsStartingInLessThanThreeMonths =
                dbSummaryReport.NumberOfRequestsStartingInLessThanThreeMonths,
            NumberOfRequestsStartingInMoreThanThreeMonths =
                dbSummaryReport.NumberOfRequestsStartingInMoreThanThreeMonths,
            AverageTimeToHandleRequests = dbSummaryReport.AverageTimeToHandleRequests,
            AllocationChangesAwaitingTaskOwnerAction = dbSummaryReport.AllocationChangesAwaitingTaskOwnerAction,
            ProjectChangesAffectingNextThreeMonths = dbSummaryReport.ProjectChangesAffectingNextThreeMonths,
            PositionsEnding = dbSummaryReport.PositionsEnding
                .Select(pe => new EndingPosition()
                {
                    Id = pe.Id,
                    FullName = pe.FullName,
                    EndDate = pe.EndDate
                })
                .ToArray(),
            PersonnelMoreThan100PercentFTE = dbSummaryReport.PersonnelMoreThan100PercentFTE
                .Select(pm => new PersonnelMoreThan100PercentFTE()
                {
                    Id = pm.Id,
                    FullName = pm.FullName,
                    FTE = pm.FTE
                }).ToArray()
        };
    }
}

public class PersonnelMoreThan100PercentFTE
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public required int FTE { get; set; }
}

public class EndingPosition
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public required DateTime EndDate { get; set; }
}

public enum SummaryReportPeriod
{
    Weekly
}