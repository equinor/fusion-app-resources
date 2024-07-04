// See https://aka.ms/new-console-template for more information
public interface IAccountResolver
{
    Task<Guid?> ResolveAccountAsync(string identifier, bool returnNullOnAmbigiousMatch);
    Task<Guid?> ResolveAppRegServicePrincipalAsync(string identifier);
}
