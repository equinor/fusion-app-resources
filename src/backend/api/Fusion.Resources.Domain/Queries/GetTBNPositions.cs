using Fusion.ApiClients.Org;
using Fusion.Integration.Http;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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

                var sourceDepartment = new DepartmentPath(request.Department);

                foreach (var pos in positions)
                {
                    foreach (var instance in pos.Instances)
                    {
                        if (instance.AssignedPerson is not null) continue;

                        // This should be some sort of configuration in the future
                        if (sourceDepartment.IsRelevant(pos.BasePosition.Department))
                        {
                            tbnPositions.Add(new QueryTbnPosition(pos, instance));
                        }
                    }

                }

                return tbnPositions;
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
