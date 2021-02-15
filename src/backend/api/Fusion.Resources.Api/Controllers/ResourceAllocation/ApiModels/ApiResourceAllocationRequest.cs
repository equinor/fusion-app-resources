using System;
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
            Type = $"{query.Type}";


            if (query.ProposedPerson != null)
                ProposedPerson = new ApiPerson(query.ProposedPerson);

            Project = new ApiProjectReference(query.Project);

            if (query.OrgPosition != null)
                OrgPosition = query.OrgPosition;

            if (query.OrgPositionInstance != null)
                OrgPositionInstance = query.OrgPositionInstance;

            AdditionalNote = query.AdditionalNote;
            if (query.ProposedChanges.Count > 0)
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
        /// <summary>Type of request
        /// <para>Check valid values used in request model <see cref="ApiAllocationRequestType"/> for information.</para>
        /// </summary>
        public string Type { get; set; }
        public ApiWorkflow? Workflow { get; set; }
        public ApiProjectReference Project { get; set; }
        public ApiPositionV2? OrgPosition { get; set; }
        public ApiPositionInstanceV2? OrgPositionInstance { get; set; }
        public string? AdditionalNote { get; set; }

        public ApiPropertiesCollection? ProposedChanges { get; set; }
        public ApiPerson? ProposedPerson { get; set; }

        public DateTimeOffset Created { get; set; }
        public ApiPerson CreatedBy { get; set; }

        public DateTimeOffset? Updated { get; set; }
        public ApiPerson? UpdatedBy { get; set; }

        public DateTimeOffset? LastActivity { get; set; }
        public bool IsDraft { get; set; }

        public ApiProvisioningStatus ProvisioningStatus { get; set; }

    }
}
