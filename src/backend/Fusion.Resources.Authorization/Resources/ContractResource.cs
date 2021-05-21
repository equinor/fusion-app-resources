using Fusion.Resources.Domain;
using System;


namespace Fusion.Resources.Api.Authorization
{
    public class ContractResource
    {
        public ContractResource(ProjectIdentifier project, Guid contract)
        {
            Project = project;
            Contract = contract;
        }

        public ProjectIdentifier Project { get; }
        public Guid Contract { get; }
    }
}
