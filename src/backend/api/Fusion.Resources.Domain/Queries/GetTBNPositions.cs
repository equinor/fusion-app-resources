using Fusion.ApiClients.Org;
using Fusion.Integration.Http;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
            private readonly IHttpClientFactory httpClientFactory;
            private readonly IProjectOrgResolver projectOrgResolver;
            private readonly ResourcesDbContext db;
            private readonly JsonSerializerOptions options;

            public Handler(IHttpClientFactory httpClientFactory, IProjectOrgResolver projectOrgResolver, ResourcesDbContext db)
            {
                this.httpClientFactory = httpClientFactory;
                this.projectOrgResolver = projectOrgResolver;
                this.db = db;
                this.options = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                };
                this.options.Converters.Add(new JsonStringEnumConverter());
            }

            public async Task<IEnumerable<QueryTbnPosition>> Handle(GetTbnPositions request, CancellationToken cancellationToken)
            {
                const string tbn_endpoint = "/admin/positions/tbn";
                const string org_client_name = "Org.Integration.Application";

                var matrix = await db.ResponsibilityMatrices
                    .Include(row => row.Project)
                    .Where(row => row.Unit == request.Department && row.Project != null)
                    .ToListAsync();

                var departmentProjects = new HashSet<Guid>(
                    matrix.Select(row => row.Project!.OrgProjectId).Distinct()
                );

                var client = httpClientFactory.CreateClient(org_client_name); ;

                var result = await client.GetAsync(tbn_endpoint, cancellationToken);
                if(!result.IsSuccessStatusCode)
                {                    
                    throw new IntegrationError("Failed to retrieve tbn positions from org service.", new OrgApiError(result,  await result.Content.ReadAsStringAsync()));
                }

                var positions = await JsonSerializer.DeserializeAsync<ApiPositionV2[]>(
                    await result.Content.ReadAsStreamAsync(cancellationToken),
                    options,
                    cancellationToken: cancellationToken
                );

                var requestRouter = new RequestRouter(db);
                var tbnPositions = new List<QueryTbnPosition>();

                foreach (var pos in positions!)
                {
                    if (!departmentProjects.Contains(pos.ProjectId)) continue;

                    foreach (var instance in pos.Instances)
                    {
                        if (instance.AssignedPerson is not null) continue;

                        var project = await projectOrgResolver.ResolveProjectAsync(pos.ProjectId);
                        if (project is null) continue;


                        // This logic will not scale, if there is ex. 1k tbn positions in the system, this will kill the database, with 1 round trip pr instance, pr api request.
                        //var department = await requestRouter.Route(pos, instance, cancellationToken);

                        if (IsRelevantBasePositionDepartment(request.Department, pos.BasePosition.Department))
                        {
                            tbnPositions.Add(new QueryTbnPosition(pos, instance));
                        }
                    }

                }

                return tbnPositions;
            }

            private static bool IsRelevantBasePositionDepartment(string sourceDepartment, string basePositionDepartment)
            {
                if (sourceDepartment is null || basePositionDepartment is null)
                    return false;

                if (sourceDepartment.StartsWith(basePositionDepartment, System.StringComparison.OrdinalIgnoreCase))
                    return true;

                if (basePositionDepartment.StartsWith(sourceDepartment, System.StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }

        }
    }
}
