#nullable enable
using Fusion.ApiClients.Org;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients
{
    public interface IResourcesApiClient
    {
        Task<IEnumerable<ProjectReference>> GetProjectsAsync();
        Task<IEnumerable<ResourceAllocationRequest>> GetIncompleteDepartmentAssignedResourceAllocationRequestsForProjectAsync(ProjectReference project);
        Task<bool> ReassignRequestAsync(ResourceAllocationRequest item, string? department);
        Task<IEnumerable<ResourceAllocationRequest>> GetAllRequestsForDepartment(string departmentIdentifier);
        Task<IEnumerable<InternalPersonnelPerson>> GetAllPersonnelForDepartment(string departmentIdentifier);

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
            public Workflow? workflow { get; set; }
        }

        public enum RequestState
        {
            approval, proposal, provisioning, created, completed
        }

        public class Workflow
        {
            public string LogicAppName { get; set; }
            public string LogicAppVersion { get; set; }

            [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
            public ApiWorkflowState State { get; set; }

            public IEnumerable<WorkflowStep> Steps { get; set; }

            public enum ApiWorkflowState { Running, Canceled, Error, Completed, Terminated, Unknown }

        }

        public class WorkflowStep
        {
            public string Id { get; set; }
            public string Name { get; set; }

            public bool IsCompleted => Completed.HasValue;

            /// <summary>
            /// Pending, Approved, Rejected, Skipped
            /// </summary>
            [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
            public ApiWorkflowStepState State { get; set; }

            public DateTimeOffset? Started { get; set; }
            public DateTimeOffset? Completed { get; set; }
            public DateTimeOffset? DueDate { get; set; }
            public InternalPersonnelPerson? CompletedBy { get; set; }
            public string Description { get; set; }
            public string? Reason { get; set; }

            public string? PreviousStep { get; set; }
            public string? NextStep { get; set; }

            public enum ApiWorkflowStepState { Pending, Approved, Rejected, Skipped, Unknown }
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
            public Guid? InternalId { get; set; }
            public string? Name { get; set; }
        }

        public class InternalPersonnelPerson
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
            public List<PersonnelPosition> PositionInstances { get; set; } = new List<PersonnelPosition>();
        }

        public class PersonnelPosition
        {
            public Guid? PositionId { get; set; }
            public Guid? InstanceId { get; set; }
            public DateTime? AppliesFrom { get; set; }
            public DateTime? AppliesTo { get; set; }
            public string? Name { get; set; } = null!;
            public string? Location { get; set; }
            public string? AllocationState { get; set; }
            public DateTime? AllocationUpdated { get; set; }

            public bool IsActive => AppliesFrom <= DateTime.UtcNow.Date && AppliesTo >= DateTime.UtcNow.Date;
            public double Workload { get; set; }
            public ProjectReference? Project { get; set; }
        }
        #endregion Models
    }
}