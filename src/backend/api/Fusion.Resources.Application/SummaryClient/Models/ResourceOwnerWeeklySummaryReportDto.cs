using System;

namespace Fusion.Resources.Application.SummaryClient.Models;

/// <summary>
///     This represents the DTO/ApiModel returned from the summary api.
/// </summary>
public class ResourceOwnerWeeklySummaryReportDto
{
    public required Guid Id { get; set; }
    public string DepartmentSapId { get; set; } = string.Empty;
    public DateTime Period { get; set; }
    public string NumberOfPersonnel { get; set; } = string.Empty;
    public string CapacityInUse { get; set; } = string.Empty;
    public string NumberOfRequestsLastPeriod { get; set; } = string.Empty;
    public string NumberOfOpenRequests { get; set; } = string.Empty;
    public string NumberOfRequestsStartingInLessThanThreeMonths { get; set; } = string.Empty;
    public string NumberOfRequestsStartingInMoreThanThreeMonths { get; set; } = string.Empty;
    public string AverageTimeToHandleRequests { get; set; } = string.Empty;
    public string AllocationChangesAwaitingTaskOwnerAction { get; set; } = string.Empty;

    public string ProjectChangesAffectingNextThreeMonths { get; set; } = string.Empty;

    public ApiEndingPosition[] PositionsEnding { get; set; } = [];

    public ApiPersonnelMoreThan100PercentFTE[] PersonnelMoreThan100PercentFTE { get; set; } = [];


    public class ApiPersonnelMoreThan100PercentFTE
    {
        public string FullName { get; set; } = string.Empty;
        public double FTE { get; set; }
    }

    public class ApiEndingPosition
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
    }
}