using System;


namespace Fusion.Resources.Api.Authorization
{
    public class ContractResource
    {
        public ContractResource(Controllers.ProjectIdentifier project, Guid contract)
        {
            Project = project;
            Contract = contract;
        }

        public Controllers.ProjectIdentifier Project { get; }
        public Guid Contract { get; }
    }
}
