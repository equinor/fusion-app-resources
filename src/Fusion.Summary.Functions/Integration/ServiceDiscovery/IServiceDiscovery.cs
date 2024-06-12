using System.Threading.Tasks;

namespace Fusion.Summary.Functions
{
    public interface IServiceDiscovery
    {
        Task<string> ResolveServiceAsync(ServiceEndpoint endpoint);
    }
}
