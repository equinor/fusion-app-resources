using Fusion.Resources.Database;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Diagnostics;
using Fusion.Integration.Org;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FluentValidation;
using Fusion.Resources.Domain.Commands.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetResourceAllocationRequests : IRequest<QueryRangedList<QueryResourceAllocationRequest>>
    {
        public GetResourceAllocationRequests(ODataQueryParams? query = null)
        {
            this.Query = query ?? new ODataQueryParams();

            if (Query.ShouldExpand("OrgPosition"))
                Expands |= ExpandFields.OrgPosition;
            if (Query.ShouldExpand("OrgPositionInstance"))
                Expands |= ExpandFields.OrgPositionInstance;
            if (Query.ShouldExpand("DepartmentDetails"))
                Expands |= ExpandFields.DepartmentDetails;
            if (Query.ShouldExpand("Actions"))
                Expands |= ExpandFields.Actions;
        }

        public GetResourceAllocationRequests WithProjectId(Guid projectId)
        {
            ProjectId = projectId;
            return this;
        }

        public GetResourceAllocationRequests WithPositionId(Guid positionId)
        {
            Expands |= ExpandFields.OrgPosition;
            Expands |= ExpandFields.OrgPositionInstance;
            PositionId = positionId;

            return this;
        }

        public GetResourceAllocationRequests ExpandPositions(bool shouldExpand = true)
        {
            if (shouldExpand)
                Expands |= ExpandFields.OrgPosition;
            return this;
        }

        public GetResourceAllocationRequests ExpandPositionInstances(bool shouldExpand = true)
        {
            if (shouldExpand)
                Expands |= ExpandFields.OrgPositionInstance;
            return this;
        }

        public GetResourceAllocationRequests WithAssignedDepartment(string departmentString)
        {
            DepartmentString = departmentString;
            return this;
        }

        public GetResourceAllocationRequests SharedWith(PersonId azureUniqueId)
        {
            PersonId = azureUniqueId;
            return this;
        }

        /// <summary>
        /// Only include unassigned requests in the result
        /// </summary>
        public GetResourceAllocationRequests WithUnassignedFilter(bool onlyIncludeUnassigned)
        {
            Unassigned = onlyIncludeUnassigned;
            return this;
        }

        public GetResourceAllocationRequests WithOnlyCount(bool onlyReturnCount)
        {
            OnlyCount = onlyReturnCount;
            return this;
        }

        public GetResourceAllocationRequests WithExcludeCompleted(bool exclude = true)
        {
            ExcludeCompleted = exclude;
            return this;
        }

        public GetResourceAllocationRequests ForResourceOwners()
        {
            Owner = DbInternalRequestOwner.ResourceOwner;
            Recipient = QueryMessageRecipient.ResourceOwner;
            Responsible = QueryTaskResponsible.ResourceOwner;
            return this;
        }
        public GetResourceAllocationRequests ForTaskOwners()
        {
            Owner = DbInternalRequestOwner.Project;
            Recipient = QueryMessageRecipient.TaskOwner;
            Responsible = QueryTaskResponsible.TaskOwner;
            return this;
        }

        public GetResourceAllocationRequests ForAll(bool? shouldIncludeAllRequests = true)
        {
            ShouldIncludeAllRequests = shouldIncludeAllRequests;
            return this;
        }

        public GetResourceAllocationRequests WithExcludeWithoutProposedPerson()
        {
            ExcludeWithoutProposedPerson = true;
            return this;
        }

        public GetResourceAllocationRequests WithActionCount()
        {
            Expands |= ExpandFields.ActionCount;
            return this;
        }

        public Guid? ProjectId { get; private set; }
        public string? DepartmentString { get; private set; }
        public bool Unassigned { get; private set; }
        public bool OnlyCount { get; private set; }
        public bool? ExcludeCompleted { get; private set; }

        private DbInternalRequestOwner? Owner { get; set; }
        private QueryMessageRecipient Recipient { get; set; }
        private QueryTaskResponsible Responsible { get; set; }
        private ODataQueryParams Query { get; set; }
        private ExpandFields Expands { get; set; }

        /// <summary>
        /// Use <see cref="ForAll(bool?)"/>
        /// </summary>
        public bool? ShouldIncludeAllRequests { get; private set; }
        public bool ExcludeWithoutProposedPerson { get; private set; }

        /// <summary>
        /// Use <see cref="WithPositionId(Guid)"/>
        /// </summary>
        public Guid? PositionId { get; private set; }
        public PersonId? PersonId { get; private set; }

        [Flags]
        private enum ExpandFields
        {
            None                =      0,
            OrgPosition         = 1 << 0,
            OrgPositionInstance = 1 << 1,
            DepartmentDetails   = 1 << 2,
            Actions             = 1 << 3,
            ActionCount         = 1 << 4
        }

        public class Validator : AbstractValidator<GetResourceAllocationRequests>
        {
            public Validator()
            {
                RuleFor(x => x.Owner)
                    .Must(o => o.HasValue).When(x => !x.ShouldIncludeAllRequests.HasValue)
                    .WithMessage("GetResourceAllocationRequests must be scoped with either `ForAll()`, `ForResourceOwner`, or `ForTaskOwner()`");

                RuleFor(x => x.ShouldIncludeAllRequests)
                    .Must(o => o.HasValue).When(x => !x.Owner.HasValue)
                    .WithMessage("GetResourceAllocationRequests must be scoped with either `ForAll()`, `ForResourceOwner`, or `ForTaskOwner()`");
            }
        }

        public class Handler : IRequestHandler<GetResourceAllocationRequests, QueryRangedList<QueryResourceAllocationRequest>>
        {
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IMediator mediator;
            private const int DefaultPageSize = 100;
            private readonly IFusionLogger<GetResourceAllocationRequests> log;

            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver, IMediator mediator, IFusionLogger<GetResourceAllocationRequests> log)
            {
                this.db = db;
                this.orgResolver = orgResolver;
                this.mediator = mediator;
                this.log = log;
            }

            public async Task<QueryRangedList<QueryResourceAllocationRequest>> Handle(GetResourceAllocationRequests request, CancellationToken cancellationToken)
            {

                var query = db.ResourceAllocationRequests
                    .Include(r => r.OrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .AsQueryable();

                query = ApplySorting(query, request.Query);

                if (request.Owner is not null)
                    query = query.Where(r => r.IsDraft == false || r.RequestOwner == request.Owner);

                if (request.ExcludeCompleted.GetValueOrDefault(false))
                    query = query.Where(c => c.State.IsCompleted == false);

                if (request.Query.HasFilter)
                {
                    query = query.ApplyODataFilters(request.Query, m =>
                    {
                        // These fields are provided by the api, so must match the api model fields unfortunately. This is a known limitation, however 
                        // it is done instead of having to handle complexities in query expression logic..
                        m.MapField("assignedDepartment", i => i.AssignedDepartment);
                        m.MapField("discipline", i => i.Discipline);
                        m.MapField("isDraft", i => i.IsDraft);
                        m.MapField("project.id", i => i.Project.OrgProjectId);
                        m.MapField("updated", i => i.Updated);
                        m.MapField("state", i => i.State.State);
                        m.MapField("state.isComplete", i => i.State.IsCompleted);
                        m.MapField("provisioningStatus.state", i => i.ProvisioningStatus.State);
                        m.MapField("proposedPerson.azureUniqueId", x => x.ProposedPerson.AzureUniqueId);
                        m.MapField("orgPositionId", i => i.OrgPositionId);
                        m.MapField("correlationId", i => i.CorrelationId);
                    });
                }

                if (request.ProjectId.HasValue)
                    query = query.Where(c => c.Project.OrgProjectId == request.ProjectId);

                if (request.DepartmentString != null)
                    query = query.Where(c => c.AssignedDepartment == request.DepartmentString);
                if (request.Unassigned)
                    query = query.Where(c => c.AssignedDepartment == null);
                if (request.ExcludeWithoutProposedPerson)
                    query = query.Where(x => x.ProposedPerson.HasBeenProposed);
                if (request.PositionId.HasValue)
                    query = query.Where(r => r.OrgPositionId == request.PositionId.Value);
                if (request.PersonId.HasValue)
                {
                    if(request.PersonId.Value.Type == Domain.PersonId.IdentifierType.Mail)
                        query = query.Where(r => db.SharedRequests.Any(x => x.RequestId == r.Id && x.SharedWith.Mail == request.PersonId.Value.Mail));
                    else
                        query = query.Where(r => db.SharedRequests.Any(x => x.RequestId == r.Id && x.SharedWith.AzureUniqueId == request.PersonId.Value.UniqueId));
                }


                var skip = request.Query.Skip.GetValueOrDefault(0);
                var take = request.Query.Top.GetValueOrDefault(DefaultPageSize);


                var countOnly = request.OnlyCount;

                var pagedQuery = await QueryRangedList.FromQueryAsync(query.Select(x => new QueryResourceAllocationRequest(x, null)), skip, take, countOnly);


                if (!countOnly)
                {
                    await AddWorkFlows(pagedQuery);
                    await AddProposedPersons(pagedQuery);
                    await AddOrgPositions(pagedQuery, request.Expands);
                    await AddDepartmentDetails(pagedQuery, request.Expands);
                    await AddActions(pagedQuery, request);
                    await AddActionCount(pagedQuery, request);
                }

                return pagedQuery;
            }

            private async Task AddActions(QueryRangedList<QueryResourceAllocationRequest> pagedQuery, GetResourceAllocationRequests request)
            {
                if (!request.Expands.HasFlag(ExpandFields.Actions)) return;

                var requestActions = await mediator.Send(new GetActionsForRequests(pagedQuery.Select(x => x.RequestId), request.Responsible));
                foreach(var rq in pagedQuery)
                {
                    if (requestActions.Contains(rq.RequestId))
                        rq.Actions = requestActions[rq.RequestId].ToList();
                }
            }

            private async Task AddActionCount(QueryRangedList<QueryResourceAllocationRequest> pagedQuery, GetResourceAllocationRequests request)
            {
                if (!request.Expands.HasFlag(ExpandFields.ActionCount)) return;

                var actionCounts = await mediator.Send(new CountActionsForRequests(pagedQuery.Select(x => x.RequestId), request.Responsible));
                foreach (var rq in pagedQuery)
                {
                    if (actionCounts.TryGetValue(rq.RequestId, out var count))
                        rq.ActionCount = count;
                }
            }

            private IQueryable<DbResourceAllocationRequest> ApplySorting(IQueryable<DbResourceAllocationRequest> query, ODataQueryParams odataQuery)
            {
                if (odataQuery.OrderBy.Any())
                {
                    // Limited support, only one lever supported... 

                    var mainOrder = odataQuery.OrderBy.First();
                    switch (mainOrder.Field)
                    {
                        case "id": return mainOrder.Direction == SortDirection.desc ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id);
                        case "orgPositionId": return mainOrder.Direction == SortDirection.desc ? query.OrderByDescending(e => e.OrgPositionId) : query.OrderBy(e => e.OrgPositionId);
                        case "number": return mainOrder.Direction == SortDirection.desc ? query.OrderByDescending(e => e.RequestNumber) : query.OrderBy(e => e.RequestNumber);
                        case "created": return mainOrder.Direction == SortDirection.desc ? query.OrderByDescending(e => e.Created) : query.OrderBy(e => e.Created);
                        case "updated": return mainOrder.Direction == SortDirection.desc ? query.OrderByDescending(e => e.Updated) : query.OrderBy(e => e.Updated);
                        case "lastActivity": return mainOrder.Direction == SortDirection.desc ? query.OrderByDescending(e => e.LastActivity) : query.OrderBy(e => e.LastActivity);

                        default:
                            throw new InvalidOperationException("Unsupported sorting field");
                    }
                }

                return query.OrderBy(r => r.Id);
            }

            private async Task AddDepartmentDetails(QueryRangedList<QueryResourceAllocationRequest> pagedQuery, ExpandFields expands)
            {
                if (!expands.HasFlag(ExpandFields.DepartmentDetails)) return;

                var relevantDepartmentIds = pagedQuery
                    .Where(r => !string.IsNullOrEmpty(r.AssignedDepartment))
                    .Select(r => r.AssignedDepartment!)
                    .Distinct();

                var departments = await mediator.Send(new GetDepartments()
                    .ByIds(relevantDepartmentIds.ToArray())
                    .ExpandDelegatedResourceOwners()
                );

                var departmentMap = departments.ToDictionary(dpt => dpt.DepartmentId);

                foreach (var req in pagedQuery)
                {
                    // Assigned Department on request may not be valid anymore. Make sure to check if dictionary actually contains the value before trying to apply details.
                    if (string.IsNullOrEmpty(req.AssignedDepartment) || !departmentMap.ContainsKey(req.AssignedDepartment)) continue;
                    req.AssignedDepartmentDetails = departmentMap[req.AssignedDepartment];
                }
            }

            private async Task AddOrgPositions(List<QueryResourceAllocationRequest> requestItems, ExpandFields expands)
            {
                if ((expands.HasFlag(ExpandFields.OrgPosition) || expands.HasFlag(ExpandFields.OrgPositionInstance)) == false)
                    return;

                // Expand org position.
                var resolvedOrgChartPositions =
                    (await orgResolver.ResolvePositionsAsync(requestItems.Where(r => r.OrgPositionId.HasValue)
                        .Select(r => r.OrgPositionId!.Value))).ToList();

                // If none resolved, return.
                if (!resolvedOrgChartPositions.Any())
                    return;

                foreach (var req in requestItems)
                {
                    if (req.OrgPositionId == null) continue;

                    var position = resolvedOrgChartPositions.FirstOrDefault(p => p.Id == req.OrgPositionId);

                    if (position != null)
                    {
                        req.WithResolvedOriginalPosition(position, expands.HasFlag(ExpandFields.OrgPositionInstance) ? req.OrgPositionInstanceId : null);
                    }
                }
            }

            private async Task AddWorkFlows(List<QueryResourceAllocationRequest> requestItems)
            {
                var workFlows = await mediator.Send(new GetRequestWorkflows(requestItems.Select(r => r.RequestId)));
                var workFlowList = workFlows.ToList();

                foreach (var req in requestItems)
                {
                    var wf = workFlowList.FirstOrDefault(x => x.RequestId == req.RequestId);
                    if (wf == null)
                    {
                        // log critical event
                        log.LogCritical($"Workflow not found for request id: {req.RequestId}");
                        continue;
                    }
                    req.Workflow = wf;
                }
            }

            private async Task AddProposedPersons(List<QueryResourceAllocationRequest> requestItems)
            {
                var ids = requestItems
                    .Where(r => r.ProposedPerson is not null)
                    .Select(r => r.ProposedPerson!.AzureUniqueId)
                    .Distinct();

                var profiles = await mediator.Send(new GetPersonProfiles(ids));

                foreach (var request in requestItems)
                {
                    var id = request.ProposedPerson?.AzureUniqueId;
                    if (id is not null && profiles.ContainsKey(id.Value))
                        request.ProposedPerson!.Person = profiles[id.Value];
                }
            }
        }
    }
}