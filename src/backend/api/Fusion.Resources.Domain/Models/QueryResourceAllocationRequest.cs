using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fusion.ApiClients.Org;
using Fusion.Resources.Database.Entities;

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

            OrgPositionId = entity.OrgPositionId;
            OrgPositionInstanceId = entity.OrgPositionInstance?.Id;

            if (entity.ProposedPerson != null)
                ProposedPerson = new QueryPerson(entity.ProposedPerson);

            AdditionalNote = entity.AdditionalNote;

            ProposedChangesJson = entity.ProposedChanges;

            Created = entity.Created;
            Updated = entity.Updated;
            CreatedBy = new QueryPerson(entity.CreatedBy);
            UpdatedBy = QueryPerson.FromEntityOrDefault(entity.UpdatedBy);
            LastActivity = entity.LastActivity;
            IsDraft = entity.IsDraft;

            ProvisioningStatus = new QueryProvisioningStatus(entity.ProvisioningStatus);
        }

        internal Guid? OrgPositionId { get; set; }
        internal Guid? OrgPositionInstanceId { get; set; }

        public Guid RequestId { get; set; }
        public string? Discipline { get; set; }
        public QueryAllocationRequestType Type { get; set; }
        public QueryWorkflow? Workflow { get; set; }
        public DbRequestState State { get; set; }

        public QueryProject Project { get; set; }
        public ApiPositionV2? OrgPosition { get; set; }

        public ApiPositionInstanceV2? OrgPositionInstance { get; set; }

        public QueryPerson? ProposedPerson { get; set; }
        public string? AdditionalNote { get; set; }

        public string? ProposedChangesJson { get; set; }

        public Dictionary<string, object> ProposedChanges
        {
            get
            {
                if (ProposedChangesJson is null)
                    return new Dictionary<string, object>();

                try
                {
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(ProposedChangesJson)!;
                }
                catch
                {
                    return new Dictionary<string, object>();
                }
            }
        }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public QueryPerson CreatedBy { get; set; }
        public QueryPerson? UpdatedBy { get; set; }
        public DateTimeOffset? LastActivity { get; set; }
        public bool IsDraft { get; set; }
        public QueryProvisioningStatus ProvisioningStatus { get; set; }

        public enum QueryAllocationRequestType
        {
            Normal,
            JointVenture,
            Direct
        }

        internal QueryResourceAllocationRequest WithResolvedOriginalPosition(ApiPositionV2 position, Guid? positionInstanceId)
        {
            OrgPosition = position;
            if (positionInstanceId.HasValue)
                OrgPositionInstance = position.Instances.FirstOrDefault(x => x.Id == positionInstanceId);
            return this;
        }

    }
}