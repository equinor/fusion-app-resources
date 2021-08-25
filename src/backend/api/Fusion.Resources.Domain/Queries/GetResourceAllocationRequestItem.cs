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
            if (query.ShouldExpand("taskOwner"))
            {
                Expands |= ExpandProperties.TaskOwner;
            }
            if (query.ShouldExpand("proposedPerson.resourceOwner"))
            {
                Expands |= ExpandProperties.ResourceOwner;
            }
            if (query.ShouldExpand("departmentDetails"))
            {
                Expands |= ExpandProperties.DepartmentDetails;
            }
            if(query.ShouldExpand("actions"))
            {
                Expands |= ExpandProperties.Actions;
            }
            if(query.ShouldExpand("conversation"))
            {
                Expands |= ExpandProperties.Conversation;
            }


            return this;
        }
        public Guid RequestId { get; }


        public ExpandProperties Expands { get; set; }

        public GetResourceAllocationRequestItem ExpandAll()
        {
            Expands = ExpandProperties.All;
            return this;
        }
        public GetResourceAllocationRequestItem ExpandTaskOwner()
        {
            Expands |= ExpandProperties.TaskOwner;
            return this;
        }

        [Flags]
        public enum ExpandProperties
        {
            None                = 0,
            TaskOwner           = 1 << 0,
            ResourceOwner       = 1 << 1,
            DepartmentDetails   = 1 << 2,
            Actions               = 1 << 3,
            Conversation        = 1 << 4,
            All = TaskOwner | ResourceOwner | DepartmentDetails | Actions | Conversation,
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

                if (requestItem.OrgPositionId == null)
                    return requestItem;

                var position = await orgResolver.ResolvePositionAsync(requestItem.OrgPositionId.Value);
                if (position != null)
                {
                    requestItem.WithResolvedOriginalPosition(position, requestItem.OrgPositionInstanceId);
                }
                if (requestItem.ProposedPerson?.AzureUniqueId != null)
                    requestItem.ProposedPerson.Person = await mediator.Send(new GetPersonProfile(requestItem.ProposedPerson.AzureUniqueId));

                if (request.Expands.HasFlag(ExpandProperties.TaskOwner))
                {
                    await ExpandTaskOwnerAsync(requestItem);
                }
                if (request.Expands.HasFlag(ExpandProperties.ResourceOwner))
                {
                    await ExpandResourceOwnerAsync(requestItem);
                }
                if (request.Expands.HasFlag(ExpandProperties.DepartmentDetails))
                {
                    await ExpandDepartmentDetails(requestItem);
                }
                if(request.Expands.HasFlag(ExpandProperties.Actions))
                {
                    await ExpandActions(requestItem);
                }
                if(request.Expands.HasFlag(ExpandProperties.Conversation))
                {
                    await ExpandConversation(requestItem);
                }

                return requestItem;
            }

            private async Task ExpandConversation(QueryResourceAllocationRequest requestItem)
            {
                requestItem.Conversation = await db.RequestConversations
                    .Include(x => x.Sender)
                    .Where(x => x.RequestId == requestItem.RequestId)
                    .Select(x => new QueryConversationMessage(x))
                    .ToListAsync();
                
            }

            private async Task ExpandActions(QueryResourceAllocationRequest requestItem)
            {
                requestItem.Tasks = await db.RequestTasks
                    .Include(t => t.ResolvedBy)
                    .Include(t => t.SentBy)
                    .Where(t => t.RequestId == requestItem.RequestId)
                    .Select(t => new QueryRequestAction(t))
                    .ToListAsync();
            }

            private async Task ExpandDepartmentDetails(QueryResourceAllocationRequest requestItem)
            {
                if (String.IsNullOrEmpty(requestItem.AssignedDepartment)) return;

                try
                {
                    requestItem.AssignedDepartmentDetails = await mediator.Send(
                        new GetDepartment(requestItem.AssignedDepartment)
                           .ExpandDelegatedResourceOwners()
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Could not expand department details: {Message}", ex.Message);
                }
            }

            private async Task ExpandTaskOwnerAsync(QueryResourceAllocationRequest request)
            {
                if (request.OrgPositionId is null || request.OrgPositionInstanceId is null)
                    return;

                // If the resolving fails, let the property be null which will be an indication to the consumer that it has failed.
                try
                {
                    var taskOwnerResponse = await orgClient.GetInstanceTaskOwnerAsync(request.Project.OrgProjectId, request.OrgPositionId.Value, request.OrgPositionInstanceId.Value);

                    if (taskOwnerResponse.Value is not null)
                    {
                        request.TaskOwner = new QueryTaskOwner(taskOwnerResponse.Value.Date)
                        {
                            PositionId = taskOwnerResponse.Value.PositionId,
                            InstanceIds = taskOwnerResponse.Value.InstanceIds,
                            Persons = taskOwnerResponse.Value.Persons
                        };
                    }
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Could not resolve task owner from org chart");
                }
            }

            private async Task ExpandResourceOwnerAsync(QueryResourceAllocationRequest request)
            {
                try
                {
                    if (request.ProposedPerson?.AzureUniqueId is not null)
                    {
                        var manager = await mediator.Send(new GetResourceOwner(request.ProposedPerson.AzureUniqueId));
                        request.ProposedPerson.ResourceOwner = manager;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Could not expand resource owner: {Message}", ex.Message);
                }
            }
        }
    }
}
