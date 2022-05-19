using Fusion.Resources.Domain.Models;

namespace Fusion.Resources.Api.Authorization
{
    public static class ResourcesClaimTypes
    {
        public const string Prefix = "Fusion.Resources.Request.";
        public static string BasicRead = $"{Prefix}{SharedRequestScopes.BasicRead}";
    }
}
