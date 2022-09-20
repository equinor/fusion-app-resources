#nullable enable
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
        Task<IEnumerable<string>> GetRequestOptionsAsync(ResourceAllocationRequest item);
        Task<bool> ReassignRequestAsync(ResourceAllocationRequest item, string? department);

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
            public string? Type { get; set; }
            public string? SubType { get; set; }
            public int? Number { get; set; }
            public ApiPositionV2? OrgPosition { get; set; }
            public ApiPositionInstanceV2? OrgPositionInstance { get; set; }
            public ProposedPerson? ProposedPerson { get; set; }
            public bool HasProposedPerson => ProposedPerson?.Person.AzureUniquePersonId is not null;
        }

        public class ProposedPerson
        {
            public Person Person { get; set; } = null!;
        }

    

        public class Person
        {
            public Guid? AzureUniquePersonId { get; set; }
            public string? FullDepartment { get; set; }
            public string? Mail { get; set; }
        }

        public class ProjectReference
        {
            public Guid Id { get; set; }
        }
        #endregion Models
    }
}