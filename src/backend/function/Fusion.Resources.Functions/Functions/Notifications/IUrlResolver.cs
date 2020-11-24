using Fusion.Resources.Functions.ApiClients;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions.Notifications
{
    public interface IUrlResolver
    {
        Task<string> ResolveActiveRequestsAsync(IResourcesApiClient.ProjectContract projectContract);
    }
}