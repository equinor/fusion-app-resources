using System;
using System.Linq;
using Fusion.Resources.Application.Summary.Models;

namespace Fusion.Resources.Api.Controllers;

public class ApiSummaryReport
{
    public required Guid Id { get; set; }
    public required string DepartmentSapId { get; set; }
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

    public required ApiEndingPosition[] PositionsEnding { get; set; }

    public required ApiPersonnelMoreThan100PercentFTE[] PersonnelMoreThan100PercentFTE { get; set; }

    public static ApiSummaryReport FromQuerySummaryReport(ResourceOwnerWeeklySummaryReport weeklyWeeklySummaryReport)
    {
        return new ApiSummaryReport
        {
            Id = weeklyWeeklySummaryReport.Id,
            DepartmentSapId = weeklyWeeklySummaryReport.DepartmentSapId,
            Period = weeklyWeeklySummaryReport.Period,
            NumberOfPersonnel = weeklyWeeklySummaryReport.NumberOfPersonnel,
            CapacityInUse = weeklyWeeklySummaryReport.CapacityInUse,
            NumberOfRequestsLastPeriod = weeklyWeeklySummaryReport.NumberOfRequestsLastPeriod,
            NumberOfOpenRequests = weeklyWeeklySummaryReport.NumberOfOpenRequests,
            NumberOfRequestsStartingInLessThanThreeMonths =
                weeklyWeeklySummaryReport.NumberOfRequestsStartingInLessThanThreeMonths,
            NumberOfRequestsStartingInMoreThanThreeMonths =
                weeklyWeeklySummaryReport.NumberOfRequestsStartingInMoreThanThreeMonths,
            AverageTimeToHandleRequests = weeklyWeeklySummaryReport.AverageTimeToHandleRequests,
            AllocationChangesAwaitingTaskOwnerAction =
                weeklyWeeklySummaryReport.AllocationChangesAwaitingTaskOwnerAction,
            ProjectChangesAffectingNextThreeMonths = weeklyWeeklySummaryReport.ProjectChangesAffectingNextThreeMonths,
            PositionsEnding = weeklyWeeklySummaryReport.PositionsEnding
                .Select(pe => new ApiEndingPosition
                {
                    FullName = pe.FullName,
                    EndDate = pe.EndDate
                })
                .ToArray(),
            PersonnelMoreThan100PercentFTE = weeklyWeeklySummaryReport.PersonnelMoreThan100PercentFTE
                .Select(pe => new ApiPersonnelMoreThan100PercentFTE
                {
                    FullName = pe.FullName,
                    FTE = pe.FTE
                })
                .ToArray()
        };
    }
}

public class ApiPersonnelMoreThan100PercentFTE
{
    public required string FullName { get; set; }
    public required double FTE { get; set; }
}

public class ApiEndingPosition
{
    public required string FullName { get; set; }
    public required DateTime EndDate { get; set; }
}