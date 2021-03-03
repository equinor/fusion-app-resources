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

namespace Fusion.Resources.Domain.Queries
{
    public class GetResourceAllocationRequests : IRequest<QueryPagedList<QueryResourceAllocationRequest>>
    {

        public GetResourceAllocationRequests(ODataQueryParams? query = null)
        {
            this.Query = query ?? new ODataQueryParams();

            if (Query.ShoudExpand("OrgPosition"))
                Expands |= ExpandFields.OrgPosition;
            if (Query.ShoudExpand("OrgPositionInstance"))
                Expands |= ExpandFields.OrgPositionInstance;
            if (Query.ShoudExpand("TaskOwner"))
                Expands |= ExpandFields.TaskOwner;
        }

        public GetResourceAllocationRequests WithProjectId(Guid projectId)
        {
            ProjectId = projectId;
            return this;
        }

        public GetResourceAllocationRequests WithAssignedDepartment(string departmentString)
        {
            DepartmentString = departmentString;
            return this;
        }

        public Guid? ProjectId { get; private set; }
        public string? DepartmentString { get; private set; }
        private ODataQueryParams Query { get; set; }
        private ExpandFields Expands { get; set; }

        [Flags]
        private enum ExpandFields
        {
            None = 0,
            OrgPosition = 1 << 0,
            OrgPositionInstance = 1 << 1,
            TaskOwner = 1 << 2
        }
        public class Handler : IRequestHandler<GetResourceAllocationRequests, QueryPagedList<QueryResourceAllocationRequest>>
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

            public async Task<QueryPagedList<QueryResourceAllocationRequest>> Handle(GetResourceAllocationRequests request, CancellationToken cancellationToken)
            {

                var query = db.ResourceAllocationRequests
                    .Include(r => r.OrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .OrderBy(x => x.Id) // Should have consistent sorting due to OData criterias.
                    .AsQueryable();

                if (request.Query.HasFilter)
                {
                    query = query.ApplyODataFilters(request.Query, m =>
                    {
                        m.MapField(nameof(QueryResourceAllocationRequest.AssignedDepartment), i => i.AssignedDepartment);
                        m.MapField(nameof(QueryResourceAllocationRequest.Discipline), i => i.Discipline);
                    });
                }

                if (request.ProjectId.HasValue)
                    query = query.Where(c => c.Project.OrgProjectId == request.ProjectId);

                if (request.DepartmentString != null)
                    query = query.Where(c => c.AssignedDepartment == request.DepartmentString);

                var pagedQuery = await QueryPagedList<DbResourceAllocationRequest>.ToPagedListAsync(query,
                    request.Query.Skip.GetValueOrDefault(1), request.Query.Top.GetValueOrDefault(DefaultPageSize));

                var requestItems = new QueryPagedList<QueryResourceAllocationRequest>(pagedQuery.Select(x => new QueryResourceAllocationRequest(x)), pagedQuery.TotalCount,
                    pagedQuery.CurrentPage, pagedQuery.PageSize);

                await AddTaskOwners(requestItems, request.Expands);

                await AddWorkFlows(requestItems);

                await AddOrgPositions(requestItems, request.Expands);

                return requestItems;
            }

            private async Task AddTaskOwners(QueryPagedList<QueryResourceAllocationRequest> requestItems, ExpandFields expands)
            {
                if (expands.HasFlag(ExpandFields.TaskOwner))
                {
                    foreach (var requestItem in requestItems)
                    {
                        requestItem.TaskOwner = new QueryTaskOwner();
                        if (TemporaryRandomTrue())
                        {
                            var randomRequest = await db.ResourceAllocationRequests.OrderBy(r => Guid.NewGuid()).FirstOrDefaultAsync();
                            requestItem.TaskOwner.PositionId = randomRequest.OrgPositionId;
                            var randomPerson = await db.Persons.OrderBy(r => Guid.NewGuid()).FirstOrDefaultAsync();
                            requestItem.TaskOwner.Person = new QueryPerson(randomPerson);
                        }
                    }
                }
                static bool TemporaryRandomTrue()
                {
                    var random = new Random();
                    var outcome = random.Next(100);
                    return outcome <= 70;
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
        }
    }
}
