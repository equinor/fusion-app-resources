using System;
using System.Text.Json.Serialization;
using Fusion.ApiClients.Org;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiResourceAllocationRequest
    {
        public ApiResourceAllocationRequest(QueryResourceAllocationRequest query)
        {
            Id = query.RequestId;
            Discipline = query.Discipline;
            Type = Enum.Parse<ApiAllocationRequestType>($"{query.Type}");


            ProposedPerson = new ApiPerson(query.ProposedPerson);

            Project = new ApiProjectReference(query.Project);

            OrgPositionId = query.OrgPositionId;
            OrgPositionName = query.OrgPositionName;
            OrgPositionInstance = new ApiPositionInstance(query.OrgPositionInstance);

            AdditionalNote = query.AdditionalNote;
            ProposedChanges = new ApiPropertiesCollection(query.ProposedChanges);

            Created = query.Created;
            Updated = query.Updated;
            CreatedBy = new ApiPerson(query.CreatedBy);

            UpdatedBy = ApiPerson.FromEntityOrDefault(query.UpdatedBy);

            LastActivity = query.LastActivity;
            IsDraft = query.IsDraft;

            if (query.Workflow != null) Workflow = new ApiWorkflow(query.Workflow);
            ProvisioningStatus = new ApiProvisioningStatus(query.ProvisioningStatus);
        }

        public Guid Id { get; set; }
        public string? Discipline { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiAllocationRequestType Type { get; set; }
        public ApiWorkflow Workflow { get; set; }
        public ApiProjectReference Project { get; set; }
        public Guid? OrgPositionId { get; set; }
        public string OrgPositionName { get; set; }
        public ApiPositionInstance OrgPositionInstance { get; set; }
        public string? AdditionalNote { get; set; }

        public ApiPropertiesCollection ProposedChanges { get; set; }
        public ApiPerson ProposedPerson { get; set; }

        public DateTimeOffset Created { get; set; }
        public ApiPerson CreatedBy { get; set; }

        public DateTimeOffset? Updated { get; set; }
        public ApiPerson? UpdatedBy { get; set; }

        public DateTimeOffset LastActivity { get; set; }
        public bool IsDraft { get; set; }

        public ApiProvisioningStatus ProvisioningStatus { get; set; }

        public enum ApiAllocationRequestType { Normal, JointVenture, Direct }
    }
}
