namespace Fusion.Resources.Domain.Services.OrgClient.Abstractions;

public interface IOrgApiClientFactory
{
    public IOrgApiClient CreateClient();
}