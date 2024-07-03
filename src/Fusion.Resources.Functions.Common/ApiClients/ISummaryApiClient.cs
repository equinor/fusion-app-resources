namespace Fusion.Resources.Functions.Common.ApiClients;

public interface ISummaryApiClient
{
    public Task PutDepartmentsAsync(IEnumerable<ApiResourceOwnerDepartments> departments,
        CancellationToken cancellationToken = default);

    public Task<ICollection<ApiResourceOwnerDepartments>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default);

    public Task<ApiSummaryReport?> GetLatestWeeklyReportAsync(string departmentSapId,
        CancellationToken cancellationToken = default);
}

#region Models

// TODO: Move to shared project
// Fusion.Resources.Integration.Models ?

public class ApiResourceOwnerDepartments
{
    public ApiResourceOwnerDepartments(string departmentSapId, string fullDepartmentName,
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

public record ApiSummaryReport
{
    const string MissingValue = "-";
    public Guid Id { get; set; }
    public string DepartmentSapId { get; set; } = MissingValue;
    public string PeriodType { get; set; } = MissingValue;
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

    // may n with the list of several users (positions) - Propertybag?
    public ApiEndingPosition[] PositionsEnding { get; set; } = Array.Empty<ApiEndingPosition>();

    // may n with the list of several users - Propertybag?
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