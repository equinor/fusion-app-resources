using Fusion.Resources.Database;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Integration.Org;
using Fusion.Resources.Domain.Commands.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Fusion.Resources.Domain.Commands.Conversations;

namespace Fusion.Resources.Domain.Queries
{
    public class GetResourceAllocationRequestItem : IRequest<QueryResourceAllocationRequest?>
    {
        public GetResourceAllocationRequestItem(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public ExpandProperties Expands { get; set; }
        public QueryMessageRecipient MessageRecipient { get; private set; }
        public QueryTaskResponsible ActionResponsible { get; private set; }

        public GetResourceAllocationRequestItem WithQueryForTaskOwner(ODataQueryParams query)
        {
            if (query.ShouldExpand("taskOwner")) ExpandTaskOwner();
            if (query.ShouldExpand("proposedPerson.resourceOwner")) ExpandResourceOwner();
            if (query.ShouldExpand("departmentDetails")) ExpandDepartmentDetails();
            if (query.ShouldExpand("actions")) ExpandActions(QueryTaskResponsible.TaskOwner);
            if (query.ShouldExpand("conversation")) ExpandConversation(QueryMessageRecipient.TaskOwner);

            return this;
        }

        public GetResourceAllocationRequestItem WithQueryForResourceOwner(ODataQueryParams query)
        {
            if (query.ShouldExpand("taskOwner")) ExpandTaskOwner();
            if (query.ShouldExpand("proposedPerson.resourceOwner")) ExpandResourceOwner();
            if (query.ShouldExpand("departmentDetails")) ExpandDepartmentDetails();
            if (query.ShouldExpand("actions")) ExpandActions(QueryTaskResponsible.ResourceOwner);
            if (query.ShouldExpand("conversation")) ExpandConversation(QueryMessageRecipient.ResourceOwner);

            return this;
        }

        public GetResourceAllocationRequestItem WithQueryForBasicRead(ODataQueryParams query)
        {
            if (query.ShouldExpand("taskOwner")) ExpandTaskOwner();
            if (query.ShouldExpand("proposedPerson.resourceOwner")) ExpandResourceOwner();
            if (query.ShouldExpand("departmentDetails")) ExpandDepartmentDetails();

            return this;
        }

        public GetResourceAllocationRequestItem ExpandTaskOwner()
        {
            Expands |= ExpandProperties.TaskOwner;
            return this;
        }

        public GetResourceAllocationRequestItem ExpandResourceOwner()
        {
            Expands |= ExpandProperties.ResourceOwner;
            return this;
        }

        public GetResourceAllocationRequestItem ExpandDepartmentDetails()
        {
            Expands |= ExpandProperties.DepartmentDetails;
            return this;
        }

        public GetResourceAllocationRequestItem ExpandConversation(QueryMessageRecipient recipient)
        {
            Expands |= ExpandProperties.Conversation;
            MessageRecipient = recipient;
            return this;
        }

        public GetResourceAllocationRequestItem ExpandActions(QueryTaskResponsible responsible)
        {
            Expands |= ExpandProperties.Actions;
            ActionResponsible = responsible;
            return this;
        }

        [Flags]
        public enum ExpandProperties
        {
            None = 0,
            TaskOwner = 1 << 0,
            ResourceOwner = 1 << 1,
            DepartmentDetails = 1 << 2,
            Actions = 1 << 3,
            Conversation = 1 << 4,
        }

        public class Handler : IRequestHandler<GetResourceAllocationRequestItem, QueryResourceAllocationRequest?>
        {
            private readonly ILogger<Handler> logger;
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IMediator mediator;
            private readonly IOrgApiClient orgClient;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ILogger<Handler> logger, ResourcesDbContext db, IProjectOrgResolver orgResolver, IMediator mediator, IOrgApiClientFactory apiClientFactory, IFusionProfileResolver profileResolver)
            {
                this.logger = logger;
                this.db = db;
                this.orgResolver = orgResolver;
                this.mediator = mediator;
                this.profileResolver = profileResolver;
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
                if (request.Expands.HasFlag(ExpandProperties.Actions))
                {
                    await ExpandActions(requestItem, request.ActionResponsible);
                }
                if (request.Expands.HasFlag(ExpandProperties.Conversation))
                {
                    await ExpandConversation(requestItem, request.MessageRecipient);
                }
                return requestItem;
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
            private async Task ExpandConversation(QueryResourceAllocationRequest requestItem, QueryMessageRecipient recipient)
            {
                requestItem.Conversation = await mediator.Send(new GetRequestConversation(requestItem.RequestId, recipient));
            }

            private async Task ExpandActions(QueryResourceAllocationRequest request, QueryTaskResponsible responsible)
            {
                var actions = await mediator.Send(new GetRequestActions(request.RequestId, responsible));
                request.Actions = actions.ToList();
            }
        }
    }
}
