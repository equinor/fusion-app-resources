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
    public class GetResourceAllocationRequests : IRequest<QueryRangedList<QueryResourceAllocationRequest>>
    {

        public GetResourceAllocationRequests(ODataQueryParams? query = null)
        {
            this.Query = query ?? new ODataQueryParams();

            if (Query.ShoudExpand("OrgPosition"))
                Expands |= ExpandFields.OrgPosition;
            if (Query.ShoudExpand("OrgPositionInstance"))
                Expands |= ExpandFields.OrgPositionInstance;
        }

        public GetResourceAllocationRequests WithProjectId(Guid projectId)
        {
            ProjectId = projectId;
            return this;
        }

        public GetResourceAllocationRequests ExpandPositions(bool shouldExpand = true)
        {
            if (shouldExpand)
                Expands |= ExpandFields.OrgPosition;
            return this;
        }

        public GetResourceAllocationRequests WithAssignedDepartment(string departmentString)
        {
            DepartmentString = departmentString;
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

        //public GetResourceAllocationRequests WithExcludeDrafts(bool excludeDrafts = true)
        //{
        //    ExcludeDrafts = excludeDrafts;
        //    return this;
        //}

        public GetResourceAllocationRequests WithExcludeCompleted(bool exclude = false)
        {
            ExcludeCompleted = exclude;
            return this;
        }

        public GetResourceAllocationRequests ForResourceOwners()
        {
            Owner = DbInternalRequestOwner.ResourceOwner;
            return this;
        }
        public GetResourceAllocationRequests ForTaskOwners()
        {
            Owner = DbInternalRequestOwner.Project;
            return this;
        }

        public Guid? ProjectId { get; private set; }
        public string? DepartmentString { get; private set; }
        public bool Unassigned { get; private set; }
        public bool OnlyCount { get; private set; }
        public bool? ExcludeDrafts { get; private set; }
        public bool? ExcludeCompleted { get; private set; }

        private DbInternalRequestOwner? Owner { get; set; }

        private ODataQueryParams Query { get; set; }
        private ExpandFields Expands { get; set; }

        [Flags]
        private enum ExpandFields
        {
            None = 0,
            OrgPosition = 1 << 0,
            OrgPositionInstance = 1 << 1
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
                    .OrderBy(x => x.Id) // Should have consistent sorting due to OData criterias.
                    .AsQueryable();

                if (request.Owner is not null)
                    query = query.Where(r => r.IsDraft == false || r.RequestOwner == request.Owner);

                //if (request.ExcludeDrafts.HasValue && request.ExcludeDrafts.Value)
                //    query = query.Where(c => c.IsDraft == false);
                if (request.ExcludeCompleted.GetValueOrDefault(false))
                    query = query.Where(c => c.State.IsCompleted == false);

                if (request.Query.HasFilter)
                {
                    query = query.ApplyODataFilters(request.Query, m =>
                    {
                        m.MapField(nameof(QueryResourceAllocationRequest.AssignedDepartment), i => i.AssignedDepartment);
                        m.MapField(nameof(QueryResourceAllocationRequest.Discipline), i => i.Discipline);
                        m.MapField("isDraft", i => i.IsDraft);
                        m.MapField("project.id", i => i.Project.OrgProjectId);
                        m.MapField("updated", i => i.Updated);
                        m.MapField("state", i => i.State);
                        m.MapField("provisioningStatus.state", i => i.ProvisioningStatus.State);
                    });
                }

                

                if (request.ProjectId.HasValue)
                    query = query.Where(c => c.Project.OrgProjectId == request.ProjectId);

                if (request.DepartmentString != null)
                    query = query.Where(c => c.AssignedDepartment == request.DepartmentString);
                if (request.Unassigned)
                    query = query.Where(c => c.AssignedDepartment == null);


                var skip = request.Query.Skip.GetValueOrDefault(0);
                var take = request.Query.Top.GetValueOrDefault(DefaultPageSize);


                var countOnly = request.OnlyCount;

                var pagedQuery = await QueryRangedList.FromQueryAsync(query.Select(x => new QueryResourceAllocationRequest(x, null)), skip, take, countOnly);
                

                if (!countOnly)
                {
                    await AddWorkFlows(pagedQuery);
                    await AddOrgPositions(pagedQuery, request.Expands);
                    await AddProposedPersons(pagedQuery);
                }

                return pagedQuery;
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

                foreach(var request in requestItems)
                {
                    var id = request.ProposedPerson?.AzureUniqueId;
                    if (id is not null && profiles.ContainsKey(id.Value))
                        request.ProposedPerson!.Person = profiles[id.Value];
                }
            }
        }
    }
}
