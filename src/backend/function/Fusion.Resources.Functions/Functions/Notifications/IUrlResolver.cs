using Fusion.Resources.Functions.ApiClients;

namespace Fusion.Resources.Functions.Functions.Notifications
{
    public interface IUrlResolver
    {
        string ResolveActiveRequests(IResourcesApiClient.ProjectContract projectContract);
    }
}