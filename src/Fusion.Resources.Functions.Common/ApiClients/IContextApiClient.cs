using Fusion.Resources.Functions.Common.ApiClients.ApiModels;

namespace Fusion.Resources.Functions.Common.ApiClients;

public interface IContextApiClient
{
    public Task<ICollection<ApiContext>> GetContextsAsync(string? contextType = null, CancellationToken cancellationToken = default);
}