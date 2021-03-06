﻿using Fusion.Authorization;

namespace Fusion.Resources.Api.Authorization
{
    public class ContractorInProjectRequirement : FusionAuthorizationRequirement
    {
        public ContractorInProjectRequirement()
        {
        }

        public override string Description => $"User must be a contractor in a contract attached to the project";

        public override string Code => "ContractorInProject";

    }
}