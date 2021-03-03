using Fusion.Resources.Database;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Org;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Queries
{
    public class GetResourceAllocationRequestItem : IRequest<QueryResourceAllocationRequest?>
    {
        public GetResourceAllocationRequestItem(Guid requestId)
        {
            RequestId = requestId;
        }

        public GetResourceAllocationRequestItem WithQuery(ODataQueryParams query)
        {
            if (query.ShoudExpand("comments"))
            {
                Expands |= ExpandProperties.RequestComments;
            }
            if (query.ShoudExpand("taskOwner"))
            {
                Expands |= ExpandProperties.TaskOwner;
            }


            return this;
        }
        public Guid RequestId { get; }


        public ExpandProperties Expands { get; set; }

        [Flags]
        public enum ExpandProperties
        {
            None = 0,
            RequestComments = 1 << 0,
            TaskOwner = 1 << 1
        }

        public class Handler : IRequestHandler<GetResourceAllocationRequestItem, QueryResourceAllocationRequest?>
        {
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver, IMediator mediator)
            {
                this.db = db;
                this.orgResolver = orgResolver;
                this.mediator = mediator;
            }

            public async Task<QueryResourceAllocationRequest?> Handle(GetResourceAllocationRequestItem request, CancellationToken cancellationToken)
            {
                var row = await db.ResourceAllocationRequests
                    .Include(r => r.OrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .FirstOrDefaultAsync(c => c.Id == request.RequestId);


                if (row is null)
                    return null;

                var workflow = await mediator.Send(new GetRequestWorkflow(request.RequestId));
                var requestItem = new QueryResourceAllocationRequest(row, workflow);

                if (request.Expands.HasFlag(ExpandProperties.TaskOwner))
                {
                    requestItem.TaskOwner = new QueryTaskOwner();
                    if (TemporaryRandomTrue())
                    {
                        var randomRequest = await db.ResourceAllocationRequests.OrderBy(r => Guid.NewGuid()).FirstOrDefaultAsync();
                        requestItem.TaskOwner.PositionId = randomRequest.OrgPositionId;
                        var randomPerson = await db.Persons.OrderBy(r => Guid.NewGuid()).FirstOrDefaultAsync();
                        requestItem.TaskOwner.Person = new QueryPerson(randomPerson);
                    }
                    static bool TemporaryRandomTrue()
                    {
                        var random = new Random();
                        var outcome = random.Next(100);
                        return outcome <= 70;
                    }
                }

                if (request.Expands.HasFlag(ExpandProperties.RequestComments))
                {
                    var comments = await mediator.Send(new GetRequestComments(request.RequestId));
                    requestItem.WithComments(comments);
                }


                if (requestItem.OrgPositionId == null)
                    return requestItem;

                var position = await orgResolver.ResolvePositionAsync(requestItem.OrgPositionId.Value);
                if (position != null)
                {
                    requestItem.WithResolvedOriginalPosition(position, requestItem.OrgPositionInstanceId);
                }

                return requestItem;
            }
        }
    }
}
