using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fusion.ApiClients.Org;
using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public enum InternalRequestOwner
    {
        Project,
        ResourceOwner
    }

    public enum InternalRequestType
    {
        Allocation,
        ResourceOwnerChange,
    }

    public enum ProposalChangeScope
    {
        Default,
        InstanceOnly
    }

    public class QueryResourceAllocationRequest
    {
        public QueryResourceAllocationRequest(DbResourceAllocationRequest entity, QueryWorkflow? workflow = null)
        {
            RequestId = entity.Id;
            RequestNumber = entity.RequestNumber;
            AssignedDepartment = entity.AssignedDepartment;
            Discipline = entity.Discipline;
            Type = entity.Type.MapToDomain();

            SubType = entity.SubType;

            Workflow = workflow;
            State = entity.State.State;
            IsCompleted = entity.State.IsCompleted;

            Project = new QueryProject(entity.Project);

            OrgPositionId = entity.OrgPositionId;
            OrgPositionInstanceId = entity.OrgPositionInstance?.Id;

            if (entity.ProposedPerson.HasBeenProposed)
                ProposedPerson = new QueryProposedPerson()
                {
                    WasNotified = entity.ProposedPerson.WasNotified,
                    AzureUniqueId = entity.ProposedPerson.AzureUniqueId!.Value,
                    Mail = entity.ProposedPerson.Mail,
                    ProposedDate = entity.ProposedPerson.ProposedAt!.Value
                };


            AdditionalNote = entity.AdditionalNote;

            ProposedChangesJson = entity.ProposedChanges;
            ProposalParameters = new QueryPropsalParameters(entity.ProposalParameters);

            Created = entity.Created;
            Updated = entity.Updated;
            CreatedBy = new QueryPerson(entity.CreatedBy);
            UpdatedBy = QueryPerson.FromEntityOrDefault(entity.UpdatedBy);
            LastActivity = entity.LastActivity;
            IsDraft = entity.IsDraft;

            ProvisioningStatus = new QueryProvisioningStatus(entity.ProvisioningStatus);
            CorrelationId = entity.CorrelationId;
        }

        public Guid RequestId { get; set; }

        /// <summary>
        /// Counter to give users a reference point to a request.
        /// </summary>
        public long RequestNumber { get; set; }

        public Guid? OrgPositionId { get; set; }
        public Guid? OrgPositionInstanceId { get; set; }

        public string? AssignedDepartment { get; set; }
        public QueryDepartment? AssignedDepartmentDetails { get; set; }
        public string? Discipline { get; set; }
        public InternalRequestType Type { get; set; }
        public string? SubType { get; set; }

        public QueryWorkflow? Workflow { get; set; }
        public string? State { get; set; }
        public bool IsCompleted { get; set; }

        public QueryProject Project { get; set; }
        public ApiPositionV2? OrgPosition { get; set; }

        public ApiPositionInstanceV2? OrgPositionInstance { get; set; }

        public QueryProposedPerson? ProposedPerson { get; set; }
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
        public QueryPropsalParameters ProposalParameters { get; set; }


        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public QueryPerson CreatedBy { get; set; }
        public QueryPerson? UpdatedBy { get; set; }
        public DateTimeOffset? LastActivity { get; set; }
        public bool IsDraft { get; set; }
        public QueryProvisioningStatus ProvisioningStatus { get; set; }
        public Guid? CorrelationId { get; }
        public QueryTaskOwner? TaskOwner { get; set; }
        public List<QueryRequestAction>? Actions { get; set; }
        public List<QueryConversationMessage>? Conversation { get; set; }

        private QueryActionCounts? actionCount;
        public QueryActionCounts? ActionCount 
        { 
            get 
            {
                if (Actions is null) return actionCount;
                
                return new QueryActionCounts(Actions.Count(x => x.IsResolved), Actions.Count(x => !x.IsResolved));
            } 
            set 
            { 
                actionCount = value;
            }
        }

        public QuerySecondOpinionCounts? SecondOpinionCounts { get; set; }

        internal QueryResourceAllocationRequest WithResolvedOriginalPosition(ApiPositionV2 position, Guid? positionInstanceId)
        {
            OrgPosition = position;
            if (positionInstanceId.HasValue)
                OrgPositionInstance = position.Instances.FirstOrDefault(x => x.Id == positionInstanceId);
            return this;
        }

        public class QueryProposedPerson
        {
            public DateTimeOffset ProposedDate { get; set; }
            public Guid AzureUniqueId { get; set; }
            public string? Mail { get; set; }

            public FusionPersonProfile? Person { get; set; }

            public FusionPersonProfile? ResourceOwner { get; set; }

            public bool WasNotified { get; set; }
        }

        public class QueryPropsalParameters
        {
            public QueryPropsalParameters(DbResourceAllocationRequest.DbOpProposalParameters proposalParameters)
            {
                ChangeFrom = proposalParameters.ChangeFrom;
                ChangeTo = proposalParameters.ChangeTo;
                Scope = $"{proposalParameters.Scope}";
                ChangeType = proposalParameters.ChangeType;
            }

            public DateTime? ChangeFrom { get; set; }
            public DateTime? ChangeTo { get; set; }
            public string Scope { get; set; } 

            public string? ChangeType { get; set; }
        }
    }
}