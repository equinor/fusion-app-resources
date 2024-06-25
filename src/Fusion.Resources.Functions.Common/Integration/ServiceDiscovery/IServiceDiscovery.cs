using System.Threading.Tasks;

namespace Fusion.Resources.Functions
{
    public interface IServiceDiscovery
    {
        Task<string> ResolveServiceAsync(ServiceEndpoint endpoint);
    }
}
