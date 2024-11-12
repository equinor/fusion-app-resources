using System.Text;
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Fusion.Resources.Functions.Common.Extensions;
using Fusion.Resources.Functions.Common.Integration.Errors;
using Fusion.Resources.Functions.Common.Integration.Http;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Common.ApiClients;

public class MailApiClient : IMailApiClient
{
    private readonly HttpClient mailClient;

    public MailApiClient(IHttpClientFactory httpClientFactory)
    {
        mailClient = httpClientFactory.CreateClient(HttpClientNames.Application.Mail);
        mailClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await mailClient.PostAsync("/mails", content, cancellationToken);

        await ThrowIfNotSuccess(response);
    }

    public async Task SendEmailWithTemplateAsync(SendEmailWithTemplateRequest request, string? templateName = "default", CancellationToken cancellationToken = default)
    {
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await mailClient.PostAsync($"templates/{templateName}/mails", content, cancellationToken);

        await ThrowIfNotSuccess(response);
    }

    private async Task ThrowIfNotSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new ApiError(response.RequestMessage!.RequestUri!.ToString(), response.StatusCode, body, "Response from API call indicates error");
        }
    }
}