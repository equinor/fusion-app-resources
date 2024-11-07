using System.Diagnostics;

namespace Fusion.Resources.Functions.Common.ApiClients;

public interface ISummaryApiClient
{
    /// <exception cref="SummaryApiError"></exception>
    public Task PutDepartmentAsync(ApiResourceOwnerDepartment departments,
        CancellationToken cancellationToken = default);

    /// <exception cref="SummaryApiError"></exception>
    public Task<ICollection<ApiResourceOwnerDepartment>?> GetDepartmentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get the latest weekly summary report for a department. The report is based on the week that has passed.
    ///     If today is monday, the report is based on the last seven days (from last monday to today).
    /// </summary>
    /// <exception cref="SummaryApiError"></exception>
    public Task<ApiWeeklySummaryReport?> GetLatestWeeklyReportAsync(string departmentSapId,
        CancellationToken cancellationToken = default);

    /// <exception cref="SummaryApiError"></exception>
    public Task PutWeeklySummaryReportAsync(string departmentSapId, ApiWeeklySummaryReport report,
        CancellationToken cancellationToken = default);

    /// <exception cref="SummaryApiError" />
    public Task<ICollection<ApiProject>> GetProjectsAsync(CancellationToken cancellationToken = default);

    /// <exception cref="SummaryApiError" />
    public Task<ApiProject> PutProjectAsync(ApiProject project, CancellationToken cancellationToken = default);

    /// <exception cref="SummaryApiError"></exception>
    public Task PutWeeklyTaskOwnerReportAsync(Guid projectId, ApiWeeklyTaskOwnerReport report, CancellationToken cancellationToken = default);
}

#region Models

// TODO: Move to shared project
// Fusion.Resources.Integration.Models ?

[DebuggerDisplay("{DepartmentSapId} - {FullDepartmentName}")]
public class ApiResourceOwnerDepartment
{
    public ApiResourceOwnerDepartment()
    {
    }

    public string DepartmentSapId { get; init; } = null!;
    public string FullDepartmentName { get; init; } = null!;

    public Guid[] ResourceOwnersAzureUniqueId { get; init; } = null!;

    public Guid[] DelegateResourceOwnersAzureUniqueId { get; init; } = null!;

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
    public double FTE { get; set; } = -1;
}

public record ApiEndingPosition
{
    public string FullName { get; set; } = "-";
    public DateTime EndDate { get; set; }
}

[DebuggerDisplay("{Id} - {Name}")]
public class ApiProject
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }
    public required Guid OrgProjectExternalId { get; set; }

    public Guid? DirectorAzureUniqueId { get; set; }

    public Guid[] AssignedAdminsAzureUniqueId { get; set; } = [];
}

public class ApiWeeklyTaskOwnerReport
{
    public required Guid Id { get; set; }
    public required Guid ProjectId { get; set; }
    public required DateTime PeriodStart { get; set; }
    public required DateTime PeriodEnd { get; set; }

    public required int ActionsAwaitingTaskOwnerAction { get; set; }
    public required ApiAdminAccessExpiring[] AdminAccessExpiringInLessThanThreeMonths { get; set; }
    public required ApiPositionAllocationEnding[] PositionAllocationsEndingInNextThreeMonths { get; set; }
    public required ApiTBNPositionStartingSoon[] TBNPositionsStartingInLessThanThreeMonths { get; set; }
}

public class ApiAdminAccessExpiring
{
    public required Guid AzureUniqueId { get; set; }
    public required string FullName { get; set; }
    public required DateTime Expires { get; set; }
}

public class ApiPositionAllocationEnding
{
    public required string PositionExternalId { get; set; }

    public required string PositionName { get; set; }

    public required string PositionNameDetailed { get; set; }

    public required DateTime PositionAppliesTo { get; set; }
}

public class ApiTBNPositionStartingSoon
{
    public required string PositionExternalId { get; set; }
    public required string PositionName { get; set; }
    public required string PositionNameDetailed { get; set; }
    public required DateTime PositionAppliesFrom { get; set; }
}

#endregion