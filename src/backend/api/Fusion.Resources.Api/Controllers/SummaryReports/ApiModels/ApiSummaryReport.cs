using System;
using System.Linq;
using Fusion.Resources.Application.SummaryClient.Models;

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

    public static ApiSummaryReport FromSummaryReportDto(ResourceOwnerWeeklySummaryReportDto weeklyWeeklySummaryReportDto)
    {
        return new ApiSummaryReport
        {
            Id = weeklyWeeklySummaryReportDto.Id,
            DepartmentSapId = weeklyWeeklySummaryReportDto.DepartmentSapId,
            Period = weeklyWeeklySummaryReportDto.Period,
            NumberOfPersonnel = weeklyWeeklySummaryReportDto.NumberOfPersonnel,
            CapacityInUse = weeklyWeeklySummaryReportDto.CapacityInUse,
            NumberOfRequestsLastPeriod = weeklyWeeklySummaryReportDto.NumberOfRequestsLastPeriod,
            NumberOfOpenRequests = weeklyWeeklySummaryReportDto.NumberOfOpenRequests,
            NumberOfRequestsStartingInLessThanThreeMonths =
                weeklyWeeklySummaryReportDto.NumberOfRequestsStartingInLessThanThreeMonths,
            NumberOfRequestsStartingInMoreThanThreeMonths =
                weeklyWeeklySummaryReportDto.NumberOfRequestsStartingInMoreThanThreeMonths,
            AverageTimeToHandleRequests = weeklyWeeklySummaryReportDto.AverageTimeToHandleRequests,
            AllocationChangesAwaitingTaskOwnerAction =
                weeklyWeeklySummaryReportDto.AllocationChangesAwaitingTaskOwnerAction,
            ProjectChangesAffectingNextThreeMonths = weeklyWeeklySummaryReportDto.ProjectChangesAffectingNextThreeMonths,
            PositionsEnding = weeklyWeeklySummaryReportDto.PositionsEnding
                .Select(pe => new ApiEndingPosition
                {
                    FullName = pe.FullName,
                    EndDate = pe.EndDate
                })
                .ToArray(),
            PersonnelMoreThan100PercentFTE = weeklyWeeklySummaryReportDto.PersonnelMoreThan100PercentFTE
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