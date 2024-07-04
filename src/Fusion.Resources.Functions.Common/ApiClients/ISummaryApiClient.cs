namespace Fusion.Resources.Functions.Common.ApiClients;

public interface ISummaryApiClient
{
    public Task PutDepartmentsAsync(IEnumerable<ApiResourceOwnerDepartment> departments,
        CancellationToken cancellationToken = default);

    public Task<ICollection<ApiResourceOwnerDepartment>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get the latest weekly summary report for a department. The report is based on the last Sunday from the current
    ///     date.
    /// </summary>
    public Task<ApiWeeklySummaryReport?> GetLatestWeeklyReportAsync(string departmentSapId,
        CancellationToken cancellationToken = default);
}

#region Models

// TODO: Move to shared project
// Fusion.Resources.Integration.Models ?

public class ApiResourceOwnerDepartment
{
    public ApiResourceOwnerDepartment(string departmentSapId, string fullDepartmentName,
        Guid resourceOwnerAzureUniqueId)
    {
        DepartmentSapId = departmentSapId;
        FullDepartmentName = fullDepartmentName;
        ResourceOwnerAzureUniqueId = resourceOwnerAzureUniqueId;
    }

    public string DepartmentSapId { get; init; }
    public string FullDepartmentName { get; init; }
    public Guid ResourceOwnerAzureUniqueId { get; init; }
}

public record ApiCollection<T>(ICollection<T> Items);

public record ApiWeeklySummaryReport
{
    private const string MissingValue = "-";

    public Guid Id { get; set; }
    public string DepartmentSapId { get; set; } = MissingValue;
    public DateTime Period { get; set; }
    public string NumberOfPersonnel { get; set; } = MissingValue;
    public string CapacityInUse { get; set; } = MissingValue;
    public string NumberOfRequestsLastPeriod { get; set; } = MissingValue;
    public string NumberOfOpenRequests { get; set; } = MissingValue;
    public string NumberOfRequestsStartingInLessThanThreeMonths { get; set; } = MissingValue;
    public string NumberOfRequestsStartingInMoreThanThreeMonths { get; set; } = MissingValue;
    public string AverageTimeToHandleRequests { get; set; } = MissingValue;
    public string AllocationChangesAwaitingTaskOwnerAction { get; set; } = MissingValue;

    public string ProjectChangesAffectingNextThreeMonths { get; set; } = MissingValue;

    public ApiEndingPosition[] PositionsEnding { get; set; } = Array.Empty<ApiEndingPosition>();

    public ApiPersonnelMoreThan100PercentFTE[] PersonnelMoreThan100PercentFTE { get; set; } =
        Array.Empty<ApiPersonnelMoreThan100PercentFTE>();
}

public record ApiPersonnelMoreThan100PercentFTE
{
    public string FullName { get; set; } = "-";
    public int FTE { get; set; } = -1;
}

public record ApiEndingPosition
{
    public string FullName { get; set; } = "-";
    public DateTime EndDate { get; set; }
}

#endregion