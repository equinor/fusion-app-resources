namespace Fusion.Resources.Logic.Requests
{
    public record WorkflowAccess()
    {
        public bool IsResourceOwnerAllowed { get; init; } = false;
        public bool IsParentResourceOwnerAllowed { get; init; } = false;
        public bool IsSiblingResourceOwnerAllowed { get; init; } = false;
        public bool IsAllResourceOwnersAllowed { get; init; } = false;
        public bool IsCreatorAllowed { get; init; } = false;
        public bool IsDirectTaskOwnerAllowed { get; init; } = false;
        public bool IsOrgChartTaskOwnerAllowed { get; init; } = false;
        public bool IsOtherProjectMembersAllowed { get; init; } = false;
        public bool IsOrgChartReadAllowed { get; init; } = false;
        public bool IsOrgChartWriteAllowed { get; init; } = false;
        public bool IsOrgAdminAllowed { get; init; } = false;

        public static readonly WorkflowAccess Default = new WorkflowAccess();
    }
}
