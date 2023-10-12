#nullable enable
using Fusion.ApiClients.Org;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients
{
    public interface IResourcesApiClient
    {
        Task<IEnumerable<ProjectReference>> GetProjectsAsync();
        Task<IEnumerable<ResourceAllocationRequest>> GetIncompleteDepartmentAssignedResourceAllocationRequestsForProjectAsync(ProjectReference project);
        Task<bool> ReassignRequestAsync(ResourceAllocationRequest item, string? department);
        Task<ApiCollection<ResourceAllocationRequest>> GetAllRequestsForDepartment(string departmentIdentifier);
        Task<ApiCollection<ApiInternalPersonnelPerson>> GetAllPersonnelForDepartment(string departmentIdentifier);

        #region Models

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
            public string? State { get; set; }
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

        public class ApiCollection<T>
        {
            public ApiCollection(IEnumerable<T> items)
            {
                Value = items;
            }


            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int? TotalCount { get; set; }

            public IEnumerable<T> Value { get; set; }
        }


        public class ApiInternalPersonnelPerson
        {

            public Guid? AzureUniquePersonId { get; set; }
            public string? Mail { get; set; } = null!;
            public string? Name { get; set; } = null!;
            public string? PhoneNumber { get; set; }
            public string? JobTitle { get; set; }
            public string? OfficeLocation { get; set; }
            public string? Department { get; set; }
            public string? FullDepartment { get; set; }
            public bool IsResourceOwner { get; set; }
            public string? AccountType { get; set; }

            // Maybe used for checking if user have future positions
            //public List<ApiResourceAllocationRequest>? PendingRequests { get; }
            public List<PersonnelPosition> PositionInstances { get; set; } = new List<PersonnelPosition>();
        }

        public class PersonnelPosition
        {
            public Guid? PositionId { get; set; }
            public Guid? InstanceId { get; set; }
            public DateTime? AppliesFrom { get; set; }
            public DateTime? AppliesTo { get; set; }

            //public ApiBasePosition? BasePosition { get; set; } = null!;
            public string? Name { get; set; } = null!;
            public string? Location { get; set; }
            public string? AllocationState { get; set; }
            public DateTime? AllocationUpdated { get; set; }

            public bool IsActive => AppliesFrom <= DateTime.UtcNow.Date && AppliesTo >= DateTime.UtcNow.Date;
            public double Workload { get; set; }
        }
        #endregion Models
    }
}