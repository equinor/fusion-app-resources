using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
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
            if (query.Workflow != null)
                Workflow = new ApiWorkflow(query.Workflow);

            ProposedPerson = new ApiPerson(query.ProposedPerson);

            Project = new ApiProjectReference(query.Project);

            OrgPositionId = query.OrgPositionId;
            if (query.OrgPositionInstance != null)
                OrgPositionInstance = new ApiPositionInstance(query.OrgPositionInstance);

            AdditionalNote = query.AdditionalNote;
            ProposedChanges = query.ProposedChanges?.Select(x => new ApiProposedChange(x));

            Created = query.Created;
            Updated = query.Updated;
            CreatedBy = new ApiPerson(query.CreatedBy);

            if (query.UpdatedBy != null)
                UpdatedBy = ApiPerson.FromEntityOrDefault(query.UpdatedBy);

            LastActivity = query.LastActivity;
            IsDraft = query.IsDraft;

            //ProvisioningStatus = new ApiProvisioningStatus(query.ProvisioningStatus);
        }

        public Guid Id { get; set; }
        public string? Discipline { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiAllocationRequestType Type { get; set; }
        public ApiWorkflow Workflow { get; set; }
        public ApiProjectReference Project { get; set; }
        public Guid? OrgPositionId { get; set; }
        public ApiPositionInstance OrgPositionInstance { get; set; }
        public string? AdditionalNote { get; set; }
        public IEnumerable<ApiProposedChange>? ProposedChanges { get; set; }
        public ApiPerson ProposedPerson { get; set; }

        public DateTimeOffset Created { get; set; }
        public ApiPerson CreatedBy { get; set; }

        public DateTimeOffset? Updated { get; set; }
        public ApiPerson? UpdatedBy { get; set; }

        public DateTimeOffset LastActivity { get; set; }
        public bool IsDraft { get; set; }

        //public ApiProvisioningStatus ProvisioningStatus { get; set; }

        public enum ApiAllocationRequestType { Normal, JointVenture, Direct }
    }
}
