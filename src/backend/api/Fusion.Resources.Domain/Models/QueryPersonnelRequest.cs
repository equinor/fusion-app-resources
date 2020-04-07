using Fusion.ApiClients.Org;
using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;

#nullable enable

namespace Fusion.Resources.Domain
{
    public class QueryPersonnelRequest
    {
        public QueryPersonnelRequest(DbContractorRequest request, QueryPositionRequest position, QueryWorkflow workflow)
        {
            Id = request.Id;
            Position = position;
            Person = new QueryContractPersonnel(request.Person);

            OriginalPositionId = request.OriginalPositionId;

            Description = request.Description;
            State = request.State;
            Category = request.Category;

            Project = new QueryProject(request.Project);
            Contract = new QueryContract(request.Contract);

            Created = request.Created;
            Updated = request.Updated;
            CreatedBy = new QueryPerson(request.CreatedBy);
            UpdatedBy = QueryPerson.FromEntityOrDefault(request.UpdatedBy);
            LastActivity = request.LastActivity;

            Workflow = workflow;
            ProvisioningStatus = new QueryProvisioningStatus(request.ProvisioningStatus);
        }

        public Guid Id { get; set; }

        public DbRequestState State { get; set; }
        public DbRequestCategory Category { get; set; }

        public string Description { get; set; }


        public QueryContractPersonnel Person { get; set; }
        public QueryPositionRequest Position { get; set; }

        /// <summary>
        /// The position this request is based upon. 
        /// Null if request is of category new.
        /// </summary>
        public Guid? OriginalPositionId { get; set; }
        public ApiPositionV2? ResolvedOriginalPosition { get; set; }

        public QueryPerson CreatedBy { get; set; }
        public QueryPerson? UpdatedBy { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public DateTimeOffset LastActivity { get; set; }

        public QueryProject Project { get; set; }
        public QueryContract Contract { get; set; }

        public QueryWorkflow Workflow { get; set; }
        public QueryProvisioningStatus ProvisioningStatus { get; set; }

        public IEnumerable<QueryRequestComment>? Comments { get; set; }

        internal QueryPersonnelRequest WithResolvedOriginalPosition(ApiPositionV2 originalPosition)
        {
            OriginalPositionId = originalPosition.Id;
            ResolvedOriginalPosition = originalPosition;

            return this;
        }

        internal QueryPersonnelRequest WithComments(IEnumerable<QueryRequestComment> comments)
        {
            Comments = comments;

            return this;
        }
    }
}

