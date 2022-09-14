using Fusion.ApiClients.Org;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients
{
    public interface IResourcesApiClient
    {
        Task<IEnumerable<ProjectReference>> GetProjectsAsync();
        Task<IEnumerable<ResourceAllocationRequest>> GetIncompleteDepartmentAssignedResourceAllocationRequestsForProjectAsync(ProjectReference project);
        Task<bool> ReassignRequestAsync(ResourceAllocationRequest item, string department);
        Task<List<ProjectContract>> GetProjectContractsAsync();
        Task<PersonnelRequestList> GetTodaysContractRequests(ProjectContract projectContract, string state);
        Task<List<DelegatedRole>> RetrieveDelegatesForContractAsync(ProjectContract projectContract);

        #region Models

        public static class RequestState
        {
            public const string Created = "Created";
            public const string SubmittedToCompany = "SubmittedToCompany";
            public const string ApprovedByCompany = "ApprovedByCompany";
        }



        public class ResourceAllocationRequest
        {
            public Guid Id { get; set; }
            public string? AssignedDepartment { get; set; }
            public ApiPositionV2? OrgPosition { get; set; }
            public ApiPositionInstanceV2? OrgPositionInstance { get; set; }
            public ProposedPerson? ProposedPerson { get; set; }
            public bool HasProposedPerson => ProposedPerson?.Person.AzureUniquePersonId is not null;
        }

        public class ProposedPerson
        {
            public Person Person { get; set; } = null!;
        }

        public class ProjectContract
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string ContractNumber { get; set; }

            public Guid ProjectId { get; set; }
            public string ProjectName { get; set; }

            public Guid? ContractResponsiblePositionId { get; set; }
            public Guid? CompanyRepPositionId { get; set; }
            public Guid? ExternalContractResponsiblePositionId { get; set; }
            public Guid? ExternalCompanyRepPositionId { get; set; }
        }

        public class DelegatedRole
        {
            public string Classification { get; set; }

            public Person Person { get; set; }
        }

        public class Person
        {
            public Guid? AzureUniquePersonId { get; set; }
            public string? FullDepartment { get; set; }
            public string Mail { get; set; }
        }


        public class PersonnelRequestList
        {
            public List<PersonnelRequest> Value { get; set; }
        }

        public class PersonnelRequest
        {
            public Guid Id { get; set; }

            public string State { get; set; }

            public DateTimeOffset LastActivity { get; set; }

            public RequestPosition Position { get; set; }

            public RequestPersonnel Person { get; set; }

            public class RequestPosition
            {
                public string Name { get; set; }
                public DateTime AppliesFrom { get; set; }
                public DateTime AppliesTo { get; set; }
            }

            public class RequestPersonnel
            {
                public string Name { get; set; }
                public string Mail { get; set; }
            }
        }
        public class ProjectReference
        {
            public Guid Id { get; set; }
        }
        #endregion Models
    }
}