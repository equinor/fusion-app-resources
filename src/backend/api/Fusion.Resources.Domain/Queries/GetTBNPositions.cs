using Fusion.ApiClients.Org;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Org;
using Microsoft.Extensions.Options;

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
            private readonly IOrgApiClientFactory orgApiClientFactory;

            static Handler()
            {
                options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                options.Converters.Add(new JsonStringEnumConverter());
            }

            public Handler(IMemoryCache memoryCache, IOrgApiClientFactory orgApiClientFactory)
            {
                this.memoryCache = memoryCache;
                this.orgApiClientFactory = orgApiClientFactory;
            }

            public async Task<IEnumerable<QueryTbnPosition>> Handle(GetTbnPositions request, CancellationToken cancellationToken)
            {
                var positions = await GetTbnPositionsAsync(cancellationToken);

                var tbnPositions = new List<QueryTbnPosition>();

                foreach (var pos in positions)
                {
                    if (!IsRelevantTbnPosition(request.Department.Trim(), pos)) continue;

                    foreach (var instance in pos.Instances)
                    {
                        if (instance.AssignedPerson is not null) continue;
                        if (instance.AppliesTo < DateTime.UtcNow) continue;
                        tbnPositions.Add(new QueryTbnPosition(pos, instance));
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
            private static bool IsRelevantTbnPosition(string resourceOwnerDepartment, ApiPositionV2 position)
            {
                if (string.IsNullOrEmpty(position.BasePosition.Department)) return false;

                var basePositionDepartmentPath = new DepartmentPath(position.BasePosition.Department);

                // If project type is PRD and resourceOwner department is below L4 (L5/L6 if found), filter department as L4.
                if (position.BasePosition.ProjectType == OrgProjectType.PRD)
                {
                    // PRD type has decided to skip support positions
                    if (position.BasePosition.Name.Contains("support", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    
                }

                return basePositionDepartmentPath.IsRelevant(resourceOwnerDepartment);
            }
            private async Task<List<ApiPositionV2>> GetTbnPositionsAsync(CancellationToken cancellationToken)
            {
                const string cacheKey = "tbn-positions";

                if (memoryCache.TryGetValue(cacheKey, out List<ApiPositionV2> positions))
                    return positions;


                var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                var resp = await client.GetAsync<List<ApiPositionV2>>("/admin/positions/tbn");

                if (!resp.IsSuccessStatusCode)
                    throw new IntegrationError("Failed to retrieve tbn positions from org service.", new OrgApiError(resp.Response, resp.Content));

                memoryCache.Set(cacheKey, resp.Value, TimeSpan.FromMinutes(10));
                return resp.Value;
            }
        }
    }
}
