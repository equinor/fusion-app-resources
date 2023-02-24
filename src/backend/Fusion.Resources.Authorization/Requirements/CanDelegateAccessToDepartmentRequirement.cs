using Fusion.Authorization;
using System;

namespace Fusion.Resources.Api.Authorization.Requirements
{
    public class CanDelegateAccessToDepartmentRequirement : FusionAuthorizationRequirement
    {
        public CanDelegateAccessToDepartmentRequirement(string department)
        {
            this.Department = department;
        }

        public override string Description => "The user must be resourceowner in this or any descendant department.";
        public override string Code => "CanDelegateAccess";

        public string Department { get; }
    }
}
