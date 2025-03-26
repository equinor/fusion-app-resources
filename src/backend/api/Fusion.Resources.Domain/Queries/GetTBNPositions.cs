using Fusion.ApiClients.Org;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Queries
{
    public class GetTbnPositions : IRequest<IEnumerable<QueryTbnPosition>>
    {
        public GetTbnPositions(string department)
        {
            Department = department;
        }

        public string Department { get; }

        public class Handler : IRequestHandler<GetTbnPositions, IEnumerable<QueryTbnPosition>>
        {
            private static readonly JsonSerializerOptions options;

            private readonly IMemoryCache memoryCache;
            private readonly ResourcesDbContext db;
            private readonly IOrgApiClientFactory orgApiClientFactory;

            static Handler()
            {
                options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                options.Converters.Add(new JsonStringEnumConverter());
            }

            public Handler(IMemoryCache memoryCache, IOrgApiClientFactory orgApiClientFactory, ResourcesDbContext db)
            {
                this.memoryCache = memoryCache;
                this.orgApiClientFactory = orgApiClientFactory;
                this.db = db;
            }

            public async Task<IEnumerable<QueryTbnPosition>> Handle(GetTbnPositions request, CancellationToken cancellationToken)
            {
                var positionsResult = await GetTbnPositionsAsync(cancellationToken);

                var tbnPositions = new List<QueryTbnPosition>();

                var resourceOwnerDepartment = new DepartmentPath(request.Department);

                foreach (var result in positionsResult)
                {
                    var pos = result.Position;
                    if (!IsRelevantTbnPosition(resourceOwnerDepartment, pos)) continue;

                    foreach (var instance in pos.Instances)
                    {
                        if (instance.AssignedPerson is not null) continue;
                        if (instance.AppliesTo < DateTime.UtcNow) continue;
                        tbnPositions.Add(new QueryTbnPosition(pos, instance, result.ProjectState));
                    }
                }

                return tbnPositions;
            }

            /// <summary>
            /// Observe that PRD project type is considered a little bit different than the rest.
            /// Should be refactored to startup options, where PRD type is configured as requested.
            /// </summary>
            /// <param name="resourceOwnerDepartment"></param>
            /// <param name="position"></param>
            /// <returns></returns>
            private static bool IsRelevantTbnPosition(DepartmentPath resourceOwnerDepartment, ApiPositionV2 position)
            {
                if (string.IsNullOrEmpty(position.BasePosition.Department)) return false;

                var basePositionDepartmentPath = new DepartmentPath(position.BasePosition.Department);

                // If project type is PRD
                if (position.BasePosition.ProjectType == OrgProjectType.PRD)
                {
                    // PRD type has decided to skip support positions
                    if (position.BasePosition.Name.Contains("support", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    
                }

                // IsRelevant evaluates to true if department path is less or equal to two levels apart from origin.
                // For PRD most base position departments is on L4 level. This evaluation should cover PRD leaders on all known levels.
                return basePositionDepartmentPath.IsRelevant(resourceOwnerDepartment);
            }

            private async Task<List<GetTbnPositionResult>> GetTbnPositionsAsync(CancellationToken cancellationToken)
            {
                const string cacheKey = "tbn-positions";

                if (memoryCache.TryGetValue(cacheKey, out List<GetTbnPositionResult> positions))
                    return positions;


                var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                var resp = await client.GetAsync<List<ApiPositionV2>>("/admin/positions/tbn");

                if (!resp.IsSuccessStatusCode)
                    throw new IntegrationError("Failed to retrieve tbn positions from org service.", new OrgApiError(resp.Response, resp.Content));

                var orgProjectIds = resp.Value.Select(p => p.Project.ProjectId).Distinct();

                var projectStates = await db.Projects
                    .Select(p => new { p.OrgProjectId, p.State })
                    .Where(p => orgProjectIds.Contains(p.OrgProjectId))
                    .AsNoTracking()
                    .ToDictionaryAsync(p => p.OrgProjectId, p => p.State.ResolveProjectState(), cancellationToken: cancellationToken);

                var apiModels = resp.Value.Select(p => new GetTbnPositionResult
                {
                    Position = p,
                    ProjectState = projectStates.GetValueOrDefault(p.Project.ProjectId)
                }).ToList();

                memoryCache.Set(cacheKey, apiModels, TimeSpan.FromMinutes(10));
                return apiModels;
            }
        }

        internal class GetTbnPositionResult
        {
            public required ApiPositionV2 Position { get; set; }
            public required string? ProjectState { get; set; }
        }
    }
}
