using System;
using System.Collections.Generic;
using Fusion.Resources.Database.Entities;
using Newtonsoft.Json;

namespace Fusion.Resources.Domain
{
    public class QueryResourceAllocationRequest
    {

        public QueryResourceAllocationRequest(DbResourceAllocationRequest entity, QueryWorkflow? workflow = null)
        {
            RequestId = entity.Id;
            Discipline = entity.Discipline;
            Type = Enum.Parse<QueryAllocationRequestType>($"{entity.Type}");
            Workflow = workflow;
            State = entity.State;

            Project = new QueryProject(entity.Project);
            OrgPositionId = entity.OriginalPositionId;
            OrgPositionInstance =
                new QueryResourceAllocationRequestOrgPositionInstance(entity.ResourceAllocationOrgPositionInstance);

            ProposedPerson = new QueryPerson(entity.ProposedPerson);

            AdditionalNote = entity.AdditionalNote;

            if (entity.ProposedChanges != null)
                ProposedChanges =
                    JsonConvert
                        .DeserializeObject<
                            IEnumerable<QueryProposedChange>>(
                            entity.ProposedChanges);

            Created = entity.Created;
            Updated = entity.Updated;
            CreatedBy = new QueryPerson(entity.CreatedBy);
            UpdatedBy = QueryPerson.FromEntityOrDefault(entity.UpdatedBy);
            LastActivity = entity.LastActivity;
            IsDraft = entity.IsDraft;

            ProvisioningStatus = new QueryProvisioningStatus(entity.ProvisioningStatus);
        }

        public Guid RequestId { get; set; }
        public string? Discipline { get; set; }
        public QueryAllocationRequestType Type { get; set; }
        public QueryWorkflow? Workflow { get; set; }
        public DbRequestState State { get; set; }

        public QueryProject Project { get; set; }
        public Guid? OrgPositionId { get; set; }
        public QueryResourceAllocationRequestOrgPositionInstance OrgPositionInstance { get; set; }

        public QueryPerson ProposedPerson { get; set; }
        public string? AdditionalNote { get; set; }

        public IEnumerable<QueryProposedChange>? ProposedChanges { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public QueryPerson CreatedBy { get; set; }
        public QueryPerson? UpdatedBy { get; set; }
        public DateTimeOffset LastActivity { get; set; }
        public bool IsDraft { get; set; }
        public QueryProvisioningStatus ProvisioningStatus { get; set; }

        public enum QueryAllocationRequestType
        {
            Normal,
            JointVenture,
            Direct
        }
    }
}