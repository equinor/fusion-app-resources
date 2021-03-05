using Fusion.Resources.Database;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Org;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
            private readonly ILogger<Handler> logger;
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IMediator mediator;
            private readonly IOrgApiClient orgClient;

            public Handler(ILogger<Handler> logger, ResourcesDbContext db, IProjectOrgResolver orgResolver, IMediator mediator, IOrgApiClientFactory apiClientFactory)
            {
                this.logger = logger;
                this.db = db;
                this.orgResolver = orgResolver;
                this.mediator = mediator;
                this.orgClient = apiClientFactory.CreateClient(ApiClientMode.Application);
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

                if (request.Expands.HasFlag(ExpandProperties.TaskOwner))
                {
                    await ExpandTaskOwnerAsync(requestItem);
                }

                return requestItem;
            }

            private async Task ExpandTaskOwnerAsync(QueryResourceAllocationRequest request)
            {
                if (request.OrgPositionId is null)
                    return;

                // Get the instance and use the relevant date to resolve that task owner

                var applicableDate = request.OrgPositionInstance?.AppliesFrom ?? DateTime.UtcNow;

                // If the resolving fails, let the property be null which will be an indication to the consumer that it has failed.
                try
                {
                    var taskOwnerResponse = await orgClient.GetTaskOwnerAsync(request.Project.OrgProjectId, request.OrgPositionId.Value, applicableDate);

                    var instances = taskOwnerResponse.Value?.Instances.Where(i => i.AppliesFrom <= applicableDate.Date && i.AppliesTo >= applicableDate.Date);

                    request.TaskOwner = new QueryTaskOwner(applicableDate)
                    {
                        PositionId = taskOwnerResponse.Value?.Id,
                        InstanceIds = instances?.Select(i => i.Id).ToArray(),
                        Persons = instances?.Select(i => i.AssignedPerson).ToArray()
                    };
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Could not resolve task owner from org chart");
                }
            }
        }
    }
}
