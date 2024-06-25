namespace Fusion.Resources.Functions.Common.Integration.ServiceDiscovery
{
    public interface IServiceDiscovery
    {
        Task<string> ResolveServiceAsync(ServiceEndpoint endpoint);
    }
}
