#nullable enable
using Fusion.ApiClients.Org;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

            //TODO: Use this for checking the state of the requests
            //public ApiWorkflow? workflow { get; set; }
        }

        public class ApiWorkflow
        {
            public ApiWorkflow(QueryWorkflow workflow)
            {
                if (workflow is null)
                {
                    throw new System.ArgumentNullException(nameof(workflow));
                }

                LogicAppName = workflow.LogicAppName;
                LogicAppVersion = workflow.LogicAppVersion;

                Steps = workflow.WorkflowSteps.Select(s => new ApiWorkflowStep(s));

                State = workflow.State switch
                {
                    DbWorkflowState.Running => ApiWorkflowState.Running,
                    DbWorkflowState.Canceled => ApiWorkflowState.Canceled,
                    DbWorkflowState.Error => ApiWorkflowState.Error,
                    DbWorkflowState.Completed => ApiWorkflowState.Completed,
                    DbWorkflowState.Terminated => ApiWorkflowState.Terminated,
                    _ => ApiWorkflowState.Unknown,
                };
            }

            public string LogicAppName { get; set; }
            public string LogicAppVersion { get; set; }
            public ApiWorkflowState State { get; set; }

            public IEnumerable<ApiWorkflowStep> Steps { get; set; }

            public enum ApiWorkflowState { Running, Canceled, Error, Completed, Terminated, Unknown }

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
            public ApiProjectReference? Project { get; set; }
        }

        public class ApiProjectReference
        {
            public Guid Id { get; set; }
            public Guid? InternalId { get; set; }
            public string? Name { get; set; }
        }
        #endregion Models
    }
}