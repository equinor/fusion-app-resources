namespace Fusion.Summary.Api.Authorization;

public static class AccessRoles
{
    public static readonly string[] ResourceOwnerRoles = ["Fusion.LineOrg.Manager", "Fusion.Resources.ResourceOwner"];
    public const string ResourcesFullControl = "Fusion.Resources.FullControl";
}