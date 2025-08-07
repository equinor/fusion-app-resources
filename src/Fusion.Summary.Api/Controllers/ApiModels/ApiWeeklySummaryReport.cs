using System.Text.Json.Serialization;
using Fusion.Summary.Api.Domain.Models;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiWeeklySummaryReport
{
    public required Guid Id { get; set; }
    public required string DepartmentSapId { get; set; }
    public required DateTime Period { get; set; }
    public required DateTime PeriodEnd { get; set; }
    public required string NumberOfPersonnel { get; set; }
    public required string CapacityInUse { get; set; }
    public required string OpenRequestsWorkload { get; set; }
    public required string NumberOfRequestsLastPeriod { get; set; }
    public required string NumberOfOpenRequests { get; set; }
    public required string NumberOfRequestsStartingInLessThanThreeMonths { get; set; }
    public required string NumberOfRequestsStartingInMoreThanThreeMonths { get; set; }
    public required string AverageTimeToHandleRequests { get; set; }
    public required string AllocationChangesAwaitingTaskOwnerAction { get; set; }

    public required string ProjectChangesAffectingNextThreeMonths { get; set; }

    // may be a json with the list of several users (positions) - Propertybag?
    public required ApiEndingPosition[] PositionsEnding { get; set; }

    // may be a json with the list of several users - Propertybag?
    public required ApiPersonnelMoreThan100PercentFTE[] PersonnelMoreThan100PercentFTE { get; set; }


    public static ApiWeeklySummaryReport FromQuerySummaryReport(QueryWeeklySummaryReport queryWeeklySummaryReport)
    {
        return new ApiWeeklySummaryReport
        {
            Id = queryWeeklySummaryReport.Id,
            DepartmentSapId = queryWeeklySummaryReport.DepartmentSapId,
            Period = queryWeeklySummaryReport.Period,
            PeriodEnd = queryWeeklySummaryReport.PeriodEnd,
            NumberOfPersonnel = queryWeeklySummaryReport.NumberOfPersonnel,
            CapacityInUse = queryWeeklySummaryReport.CapacityInUse,
            OpenRequestsWorkload = queryWeeklySummaryReport.OpenRequestsWorkload,
            NumberOfRequestsLastPeriod = queryWeeklySummaryReport.NumberOfRequestsLastPeriod,
            NumberOfOpenRequests = queryWeeklySummaryReport.NumberOfOpenRequests,
            NumberOfRequestsStartingInLessThanThreeMonths =
                queryWeeklySummaryReport.NumberOfRequestsStartingInLessThanThreeMonths,
            NumberOfRequestsStartingInMoreThanThreeMonths =
                queryWeeklySummaryReport.NumberOfRequestsStartingInMoreThanThreeMonths,
            AverageTimeToHandleRequests = queryWeeklySummaryReport.AverageTimeToHandleRequests,
            AllocationChangesAwaitingTaskOwnerAction =
                queryWeeklySummaryReport.AllocationChangesAwaitingTaskOwnerAction,
            ProjectChangesAffectingNextThreeMonths = queryWeeklySummaryReport.ProjectChangesAffectingNextThreeMonths,
            PositionsEnding = queryWeeklySummaryReport.PositionsEnding
                .Select(pe => new ApiEndingPosition()
                {
                    FullName = pe.FullName,
                    EndDate = pe.EndDate
                })
                .ToArray(),
            PersonnelMoreThan100PercentFTE = queryWeeklySummaryReport.PersonnelMoreThan100PercentFTE
                .Select(pe => new ApiPersonnelMoreThan100PercentFTE()
                {
                    FullName = pe.FullName,
                    FTE = pe.FTE
                })
                .ToArray()
        };
    }
}
