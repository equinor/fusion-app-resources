using System.Text.Json.Serialization;
using Fusion.Summary.Api.Domain.Models;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiSummaryReport
{
    public required Guid Id { get; set; }
    public required string DepartmentSapId { get; set; }
    public required ApiSummaryReportPeriod PeriodType { get; set; }
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

    // may be a json with the list of several users (positions) - Propertybag?
    public required ApiEndingPosition[] PositionsEnding { get; set; }

    // may be a json with the list of several users - Propertybag?
    public required ApiPersonnelMoreThan100PercentFTE[] PersonnelMoreThan100PercentFTE { get; set; }


    public static ApiSummaryReport FromQuerySummaryReport(QuerySummaryReport querySummaryReport)
    {
        return new ApiSummaryReport
        {
            Id = querySummaryReport.Id,
            DepartmentSapId = querySummaryReport.DepartmentSapId,
            PeriodType = Enum.Parse<ApiSummaryReportPeriod>(querySummaryReport.PeriodType.ToString()),
            Period = querySummaryReport.Period,
            NumberOfPersonnel = querySummaryReport.NumberOfPersonnel,
            CapacityInUse = querySummaryReport.CapacityInUse,
            NumberOfRequestsLastPeriod = querySummaryReport.NumberOfRequestsLastPeriod,
            NumberOfOpenRequests = querySummaryReport.NumberOfOpenRequests,
            NumberOfRequestsStartingInLessThanThreeMonths =
                querySummaryReport.NumberOfRequestsStartingInLessThanThreeMonths,
            NumberOfRequestsStartingInMoreThanThreeMonths =
                querySummaryReport.NumberOfRequestsStartingInMoreThanThreeMonths,
            AverageTimeToHandleRequests = querySummaryReport.AverageTimeToHandleRequests,
            AllocationChangesAwaitingTaskOwnerAction = querySummaryReport.AllocationChangesAwaitingTaskOwnerAction,
            ProjectChangesAffectingNextThreeMonths = querySummaryReport.ProjectChangesAffectingNextThreeMonths,
            PositionsEnding = querySummaryReport.PositionsEnding
                .Select(pe => new ApiEndingPosition()
                {
                    Id = pe.Id,
                    FullName = pe.FullName,
                    EndDate = pe.EndDate
                })
                .ToArray(),
            PersonnelMoreThan100PercentFTE = querySummaryReport.PersonnelMoreThan100PercentFTE
                .Select(pe => new ApiPersonnelMoreThan100PercentFTE()
                {
                    Id = pe.Id,
                    FullName = pe.FullName,
                    FTE = pe.FTE
                })
                .ToArray()
        };
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApiSummaryReportPeriod
{
    Weekly
}