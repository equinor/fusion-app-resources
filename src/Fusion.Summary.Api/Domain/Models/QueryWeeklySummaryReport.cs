using Fusion.Summary.Api.Database.Models;

namespace Fusion.Summary.Api.Domain.Models;

public class QueryWeeklySummaryReport
{
    public required Guid Id { get; set; }
    public required string DepartmentSapId { get; set; }
    public required DateTime Period { get; set; }
    public required DateTime PeriodEnd { get; set; }
    public required string NumberOfPersonnel { get; set; }
    public required string CapacityInUse { get; set; }
    public required string NumberOfRequestsLastPeriod { get; set; }
    public required string NumberOfOpenRequests { get; set; }
    public required string NumberOfRequestsStartingInLessThanThreeMonths { get; set; }
    public required string NumberOfRequestsStartingInMoreThanThreeMonths { get; set; }
    public required string AverageTimeToHandleRequests { get; set; }
    public required string AllocationChangesAwaitingTaskOwnerAction { get; set; }
    public required string ProjectChangesAffectingNextThreeMonths { get; set; }

    public required List<EndingPosition> PositionsEnding { get; set; }
    public required List<PersonnelMoreThan100PercentFTE> PersonnelMoreThan100PercentFTE { get; set; }


    public static QueryWeeklySummaryReport FromDbSummaryReport(DbWeeklySummaryReport dbWeeklySummaryReport)
    {
        return new QueryWeeklySummaryReport
        {
            Id = dbWeeklySummaryReport.Id,
            DepartmentSapId = dbWeeklySummaryReport.DepartmentSapId,
            Period = dbWeeklySummaryReport.Period,
            PeriodEnd = dbWeeklySummaryReport.Period.AddDays(7),
            NumberOfPersonnel = dbWeeklySummaryReport.NumberOfPersonnel,
            CapacityInUse = dbWeeklySummaryReport.CapacityInUse,
            NumberOfRequestsLastPeriod = dbWeeklySummaryReport.NumberOfRequestsLastPeriod,
            NumberOfOpenRequests = dbWeeklySummaryReport.NumberOfOpenRequests,
            NumberOfRequestsStartingInLessThanThreeMonths =
                dbWeeklySummaryReport.NumberOfRequestsStartingInLessThanThreeMonths,
            NumberOfRequestsStartingInMoreThanThreeMonths =
                dbWeeklySummaryReport.NumberOfRequestsStartingInMoreThanThreeMonths,
            AverageTimeToHandleRequests = dbWeeklySummaryReport.AverageTimeToHandleRequests,
            AllocationChangesAwaitingTaskOwnerAction = dbWeeklySummaryReport.AllocationChangesAwaitingTaskOwnerAction,
            ProjectChangesAffectingNextThreeMonths = dbWeeklySummaryReport.ProjectChangesAffectingNextThreeMonths,
            PositionsEnding = dbWeeklySummaryReport.PositionsEnding
                .Select(pe => new EndingPosition()
                {
                    FullName = pe.FullName,
                    EndDate = pe.EndDate
                })
                .ToList(),
            PersonnelMoreThan100PercentFTE = dbWeeklySummaryReport.PersonnelMoreThan100PercentFTE
                .Select(pm => new PersonnelMoreThan100PercentFTE()
                {
                    FullName = pm.FullName,
                    FTE = pm.FTE
                })
                .ToList()
        };
    }

    public DbWeeklySummaryReport ToDbSummaryReport()
    {
        return new DbWeeklySummaryReport()
        {
            Id = Id,
            DepartmentSapId = DepartmentSapId,
            Period = Period,
            NumberOfPersonnel = NumberOfPersonnel,
            CapacityInUse = CapacityInUse,
            NumberOfRequestsLastPeriod = NumberOfRequestsLastPeriod,
            NumberOfOpenRequests = NumberOfOpenRequests,
            NumberOfRequestsStartingInLessThanThreeMonths =
                NumberOfRequestsStartingInLessThanThreeMonths,
            NumberOfRequestsStartingInMoreThanThreeMonths =
                NumberOfRequestsStartingInMoreThanThreeMonths,
            AverageTimeToHandleRequests = AverageTimeToHandleRequests,
            AllocationChangesAwaitingTaskOwnerAction = AllocationChangesAwaitingTaskOwnerAction,
            ProjectChangesAffectingNextThreeMonths = ProjectChangesAffectingNextThreeMonths,
            PositionsEnding = PositionsEnding
                .Select(pe => new DbEndingPosition()
                {
                    FullName = pe.FullName,
                    EndDate = pe.EndDate
                })
                .ToList(),
            PersonnelMoreThan100PercentFTE = PersonnelMoreThan100PercentFTE
                .Select(pm => new DbPersonnelMoreThan100PercentFTE()
                {
                    FullName = pm.FullName,
                    FTE = pm.FTE
                })
                .ToList()
        };
    }
}

public class PersonnelMoreThan100PercentFTE
{
    public required string FullName { get; set; }
    public required double FTE { get; set; }
}

public class EndingPosition
{
    public required string FullName { get; set; }
    public required DateTime EndDate { get; set; }
}