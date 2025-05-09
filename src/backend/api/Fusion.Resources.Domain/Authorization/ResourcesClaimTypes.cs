using Fusion.Resources.Domain.Models;

namespace Fusion.Resources
{
    public static class ResourcesClaimTypes
    {
        public const string ResourceOwnerForDepartment = "Fusion.Resources.ResourceOwnerForDepartment";
        public const string DelegatedResourceOwnerForDepartment = "Fusion.Resources.DelegatedResourceOwnerForDepartment";
        public const string Prefix = "Fusion.Resources.Request.";
        public static string BasicRead = $"{Prefix}{SharedRequestScopes.BasicRead}";
    }
}
