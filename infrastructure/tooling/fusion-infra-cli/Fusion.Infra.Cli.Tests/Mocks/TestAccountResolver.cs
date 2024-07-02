
namespace Fusion.Infra.Cli.Mocks
{
    public class TestAccountResolver : IAccountResolver
    {
        public List<ServicePrincipalAppReg> ServicePrincipals { get; set; } = new List<ServicePrincipalAppReg>();

        public Task<Guid?> ResolveAccountAsync(string identifier, bool returnNullOnAmbigiousMatch)
        {
            var appReg = ServicePrincipals.FirstOrDefault(s => string.Equals(s.DisplayName, identifier, StringComparison.OrdinalIgnoreCase));

            if (appReg is not null)
                return Task.FromResult<Guid?>(appReg.Id);

            return Task.FromResult<Guid?>(null);
        }

        public Task<Guid?> ResolveAppRegServicePrincipalAsync(string identifier)
        {
            if (Guid.TryParse(identifier, out Guid appId))
            {
                var appReg = ServicePrincipals.FirstOrDefault(s => s.AppId == appId);
                if (appReg is not null)
                    return Task.FromResult<Guid?>(appReg.Id);
            }

            return Task.FromResult<Guid?>(null);
        }
    }
}