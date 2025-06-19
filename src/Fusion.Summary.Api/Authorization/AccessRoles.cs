namespace Fusion.Summary.Api.Authorization;

public static class AccessRoles
{
    public const string ResourceOwner = "Fusion.Resources.ResourceOwner";
    public const string LineOrgManager = "Fusion.LineOrg.Manager";
    public static readonly string[] ResourceOwnerRoles = [LineOrgManager, ResourceOwner];
    public const string ResourcesFullControl = "Fusion.Resources.FullControl";
}