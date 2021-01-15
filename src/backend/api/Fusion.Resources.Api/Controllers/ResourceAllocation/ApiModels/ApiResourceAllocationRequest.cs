using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Fusion.ApiClients.Org;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiResourceAllocationRequest
    {
        public ApiResourceAllocationRequest()
        {
        }

        public ApiResourceAllocationRequest(QueryResourceAllocationRequest query)
        {
            Id = query.RequestId;
            Discipline = query.Discipline;
            Type = Enum.Parse<ApiAllocationRequestType>($"{query.Type}");
            if (query.Workflow != null)
                Workflow = new ApiWorkflow(query.Workflow);

            ProposedPerson = new ApiPersonV2
            {
                //query.ProposedPerson
                AzureUniqueId = query.ProposedPerson.AzureUniqueId,
                Mail = query.ProposedPerson.Mail
            };

            Project = new ApiProjectReferenceV2
            {
                Name = query.Project.Name,
                ProjectId = query.Project.OrgProjectId,
            };

            OrgPositionId = query.OrgPositionId;
            if (query.OrgPositionInstance != null)
                OrgPositionInstance = new ApiResourceAllocationRequestOrgPositionInstance(query.OrgPositionInstance);

            AdditionalNote = query.AdditionalNote;
            ProposedChanges = query.ProposedChanges?.Select(x => new ApiProposedChange(x));

            Created = query.Created;
            Updated = query.Updated;
            CreatedBy = new ApiPersonV2 { AzureUniqueId = query.CreatedBy.AzureUniqueId, Mail = query.CreatedBy.Mail };
            if (query.UpdatedBy != null)
                UpdatedBy = new ApiPersonV2 { AzureUniqueId = query.CreatedBy.AzureUniqueId, Mail = query.CreatedBy.Mail };

            LastActivity = query.LastActivity;
            IsDraft = query.IsDraft;

            //ProvisioningStatus = new ApiProvisioningStatus(query.ProvisioningStatus);
        }

        public Guid Id { get; set; }
        public string? Discipline { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiAllocationRequestType Type { get; set; }
        public ApiWorkflow Workflow { get; set; }
        public ApiProjectReferenceV2 Project { get; set; }
        public Guid? OrgPositionId { get; set; }
        public ApiResourceAllocationRequestOrgPositionInstance OrgPositionInstance { get; set; }
        public string? AdditionalNote { get; set; }
        public IEnumerable<ApiProposedChange>? ProposedChanges { get; set; }
        public ApiPersonV2 ProposedPerson { get; set; }

        public DateTimeOffset Created { get; set; }
        public ApiPersonV2 CreatedBy { get; set; }

        public DateTimeOffset? Updated { get; set; }
        public ApiPersonV2? UpdatedBy { get; set; }

        public DateTimeOffset LastActivity { get; set; }
        public bool IsDraft { get; set; }

        //public ApiProvisioningStatus ProvisioningStatus { get; set; }

        public enum ApiAllocationRequestType { Normal, JointVenture, Direct }
    }
}
