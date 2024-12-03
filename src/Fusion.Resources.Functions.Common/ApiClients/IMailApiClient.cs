using Fusion.Resources.Functions.Common.ApiClients.ApiModels;

namespace Fusion.Resources.Functions.Common.ApiClients;

public interface IMailApiClient
{
    public Task SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default);

    public Task SendEmailWithTemplateAsync(SendEmailWithTemplateRequest request, string? templateName = "default", CancellationToken cancellationToken = default);
}