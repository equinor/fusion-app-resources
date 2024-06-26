﻿using FluentValidation;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Itenso.TimePeriod;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetDepartmentRequestsTimeline : IRequest<QueryRequestsTimeline>
    {
        public GetDepartmentRequestsTimeline(string departmentString, DateTime timelineStart, DateTime timelineEnd, ODataQueryParams? queryParams = null)
        {
            QueryParams = queryParams;
            DepartmentString = departmentString;
            TimelineStart = timelineStart;
            TimelineEnd = timelineEnd;
            ExcludeCompleted = true;
        }

        public string DepartmentString { get; private set; }
        public ODataQueryParams? QueryParams { get; set; }
        public DateTime? TimelineStart { get; set; }
        public DateTime? TimelineEnd { get; set; }

        public bool ExcludeCompleted { get; set; }

        public GetDepartmentRequestsTimeline WithExcludeCompleted(bool exclude = true)
        {
            ExcludeCompleted = exclude;
            return this;
        }

        public class Validator : AbstractValidator<GetDepartmentRequestsTimeline>
        {
            public Validator()
            {
                RuleFor(x => x.TimelineStart).NotNull();
                RuleFor(x => x.TimelineEnd).NotNull();
                RuleFor(x => x.TimelineEnd).GreaterThan(x => x.TimelineStart);
                RuleFor(x => x.DepartmentString).NotEmpty().WithMessage("Full department string must be provided");
            }
        }

        public class Handler : IRequestHandler<GetDepartmentRequestsTimeline, QueryRequestsTimeline>
        {
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IMemoryCache deletedPositionMemoryCache;



            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver, IMemoryCache memoryCache)
            {
                this.db = db;
                this.orgResolver = orgResolver;
                this.deletedPositionMemoryCache = memoryCache;
            }

            public async Task<QueryRequestsTimeline> Handle(GetDepartmentRequestsTimeline request, CancellationToken cancellationToken)
            {
                // get requests for department
                var query = db.ResourceAllocationRequests
                    .Include(r => r.OrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .Where(r => r.IsDraft == false || r.RequestOwner == DbInternalRequestOwner.ResourceOwner)
                    .Where(r => r.AssignedDepartment == request.DepartmentString);

                if (request.ExcludeCompleted)
                    query = query.Where(c => c.State.IsCompleted == false);

                var items = await query
                    .OrderBy(x => x.Id)
                    .ToListAsync();


                var departmentRequests = new List<QueryResourceAllocationRequest>(items.Select(x => new QueryResourceAllocationRequest(x)));

                foreach (var departmentRequest in departmentRequests)
                {
                    if (departmentRequest.OrgPositionId != null)
                    {
                        // If memoryCache already contains OrgPositionId we don't want to check it again
                        if (MemCacheContains(departmentRequest.OrgPositionId))
                            continue;

                        var position = await orgResolver.ResolvePositionAsync(departmentRequest.OrgPositionId.Value);
                        if (position != null)
                        {
                            departmentRequest.WithResolvedOriginalPosition(position, departmentRequest.OrgPositionInstanceId);
                        }
                        else if (position == null && !MemCacheContains(departmentRequest.OrgPositionId))
                        {
                            // Set timeout of cache to 7 days so that we don't get an unmanageable cache size
                            deletedPositionMemoryCache.Set(departmentRequest.OrgPositionId, departmentRequest.OrgPositionId, TimeSpan.FromDays(7));
                        }
                    }
                }

                // Timeline date input has been verified in controller
                var filterStart = request.TimelineStart!.Value;
                var filterEnd = request.TimelineEnd!.Value;

                // Ensure utc dates
                if (filterStart.Kind != DateTimeKind.Utc)
                    filterStart = DateTime.SpecifyKind(filterStart, DateTimeKind.Utc);

                if (filterEnd.Kind != DateTimeKind.Utc)
                    filterEnd = DateTime.SpecifyKind(filterEnd, DateTimeKind.Utc);

                var relevantRequests = TimelineUtils.FilterRequests(departmentRequests, new TimeRange(filterStart, filterEnd)).ToList();

                var timeline = TimelineUtils.GenerateRequestsTimeline(relevantRequests, filterStart, filterEnd).OrderBy(p => p.AppliesFrom)
                            .ToList();

                var result = new QueryRequestsTimeline
                {
                    Timeline = timeline,
                    Requests = relevantRequests
                };
                return result;
            }
            private bool MemCacheContains(Guid? id)
            {
                if (id == null)
                    return false;

                return deletedPositionMemoryCache.TryGetValue(id, out id);
            }
        }
    }
}
