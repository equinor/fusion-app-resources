namespace Fusion.Resources.Domain
{
    public static class ReasonRoles
    {

        const string DelegatedManager = "DelegatedManager";
        public enum Roles
        {
            DelegatedManager,
            DelegatedParentManager,
            ParentManager,
            Manager,
            Write
        }
    }
}
