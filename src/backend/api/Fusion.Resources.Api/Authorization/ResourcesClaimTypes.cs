using Fusion.Resources.Domain.Models;

namespace Fusion.Resources.Api.Authorization
{
    public static class ResourcesClaimTypes
    {
        public static string BasicRead = $"Fusion.Resources.Request.{SharedRequestScopes.BasicRead}";
    }
}
