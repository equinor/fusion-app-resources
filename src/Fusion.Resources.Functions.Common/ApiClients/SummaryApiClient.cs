using System.Text.Json;
using Fusion.Resources.Functions.Common.Extensions;
using Fusion.Resources.Functions.Common.Integration.Errors;
using HttpClientNames = Fusion.Resources.Functions.Common.Integration.Http.HttpClientNames;

namespace Fusion.Resources.Functions.Common.ApiClients;

public class SummaryApiClient : ISummaryApiClient
{
    private readonly HttpClient summaryClient;

    private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public SummaryApiClient(IHttpClientFactory httpClientFactory)
    {
        summaryClient = httpClientFactory.CreateClient(HttpClientNames.Application.Summary);
        summaryClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task PutDepartmentAsync(ApiResourceOwnerDepartment department,
        CancellationToken cancellationToken = default)
    {
        using var body = new JsonContent(JsonSerializer.Serialize(department, jsonSerializerOptions));

        // Error logging is handled by http middleware => FunctionHttpMessageHandler
        using var response = await summaryClient.PutAsync($"departments/{department.DepartmentSapId}", body,
            cancellationToken);

        await ThrowIfUnsuccessfulAsync(response);
    }

    public async Task<ApiProject> PutProjectAsync(ApiProject project, CancellationToken cancellationToken = default)
    {
        using var body = new JsonContent(JsonSerializer.Serialize(project, jsonSerializerOptions));

        // Error logging is handled by http middleware => FunctionHttpMessageHandler
        using var response = await summaryClient.PutAsync($"projects/{project.Id}", body, cancellationToken);

        await ThrowIfUnsuccessfulAsync(response);

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return (await JsonSerializer.DeserializeAsync<ApiProject>(contentStream, jsonSerializerOptions, cancellationToken: cancellationToken))!;
    }

    public async Task<ICollection<ApiResourceOwnerDepartment>?> GetDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await summaryClient.GetAsync("departments", cancellationToken);

        await ThrowIfUnsuccessfulAsync(response);

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonSerializer.DeserializeAsync<ICollection<ApiResourceOwnerDepartment>>(contentStream,
                   jsonSerializerOptions,
                   cancellationToken: cancellationToken)
               ?? Array.Empty<ApiResourceOwnerDepartment>();
    }

    public async Task<ApiWeeklySummaryReport?> GetLatestWeeklyReportAsync(string departmentSapId,
        CancellationToken cancellationToken = default)
    {
        var lastMonday = DateTime.UtcNow.GetPreviousWeeksMondayDate();

        var queryString = $"resource-owners-summary-reports/{departmentSapId}/weekly?$filter=Period eq '{lastMonday.Date:O}'&$top=1";

        using var response = await summaryClient.GetAsync(queryString, cancellationToken);

        await ThrowIfUnsuccessfulAsync(response);


        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return (await JsonSerializer.DeserializeAsync<ApiCollection<ApiWeeklySummaryReport>>(contentStream,
            jsonSerializerOptions,
            cancellationToken: cancellationToken))?.Items?.FirstOrDefault();
    }

    public async Task<ApiWeeklyTaskOwnerReport?> GetLatestWeeklyTaskOwnerReportAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var lastMonday = DateTime.UtcNow.GetPreviousWeeksMondayDate();

        var queryString = $"/projects/{projectId}/task-owners-summary-reports/weekly?$filter=PeriodStart eq '{lastMonday.Date:O}'&$top=1";


        using var response = await summaryClient.GetAsync(queryString, cancellationToken);

        await ThrowIfUnsuccessfulAsync(response);


        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return (await JsonSerializer.DeserializeAsync<ApiCollection<ApiWeeklyTaskOwnerReport>>(contentStream,
            jsonSerializerOptions,
            cancellationToken: cancellationToken))?.Items?.FirstOrDefault();
    }

    public async Task PutWeeklySummaryReportAsync(string departmentSapId, ApiWeeklySummaryReport report,
        CancellationToken cancellationToken = default)
    {
        using var body = new JsonContent(JsonSerializer.Serialize(report, jsonSerializerOptions));

        // Error logging is handled by http middleware => FunctionHttpMessageHandler
        using var response = await summaryClient.PutAsync($"resource-owners-summary-reports/{departmentSapId}/weekly", body,
            cancellationToken);

        await ThrowIfUnsuccessfulAsync(response);
    }

    public async Task<ICollection<ApiProject>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await summaryClient.GetAsync("projects", cancellationToken);

        await ThrowIfUnsuccessfulAsync(response);

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonSerializer.DeserializeAsync<ICollection<ApiProject>>(contentStream,
            jsonSerializerOptions, cancellationToken: cancellationToken) ?? [];
    }

    public async Task PutWeeklyTaskOwnerReportAsync(Guid projectId, ApiWeeklyTaskOwnerReport report, CancellationToken cancellationToken = default)
    {
        using var body = new JsonContent(JsonSerializer.Serialize(report, jsonSerializerOptions));

        using var response = await summaryClient.PutAsync($"projects/{projectId}/task-owners-summary-reports/weekly", body, cancellationToken);

        await ThrowIfUnsuccessfulAsync(response);
    }

    private async Task ThrowIfUnsuccessfulAsync(HttpResponseMessage response)
        => await response.ThrowIfUnsuccessfulAsync((responseBody) => new SummaryApiError(response, responseBody));
}

public class SummaryApiError : ApiError
{
    public SummaryApiError(HttpResponseMessage message, string body) :
        base(message.RequestMessage?.RequestUri?.ToString() ?? "Request URI is null", message.StatusCode, body, "Error from summary api")
    {
    }
}