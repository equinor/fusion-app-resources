using System.Text.Json.Serialization;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiSummaryReport
{
    //public class ApiSummaryReport(QuerySummaryReport) {}


    public required Guid Id { get; set; }
    public required string DepartmentSapId { get; set; }
    public required ApiSummaryReportPeriod PeriodType { get; set; }

    // First day of the Period? So monday for a week. For month period type then first day of the month. UTC?
    // TODO: Time
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
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApiSummaryReportPeriod
{
    Weekly
}