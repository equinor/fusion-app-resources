using Fusion.ApiClients.Org;
using Fusion.Integration.Http;
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
    public class GetTBNPositions : IRequest<IEnumerable<TBNPosition>>
    {
        public GetTBNPositions(string department)
        {
            Department = department;
        }

        public string Department { get; }

        public class Handler : IRequestHandler<GetTBNPositions, IEnumerable<TBNPosition>>
        {
            private readonly IHttpClientFactory httpClientFactory;
            private readonly ResourcesDbContext db;
            private readonly JsonSerializerOptions options;

            public Handler(IHttpClientFactory httpClientFactory, ResourcesDbContext db)
            {
                this.httpClientFactory = httpClientFactory;
                this.db = db;
                this.options = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                };
                this.options.Converters.Add(new JsonStringEnumConverter());
            }

            public async Task<IEnumerable<TBNPosition>> Handle(GetTBNPositions request, CancellationToken cancellationToken)
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

                var positions = await JsonSerializer.DeserializeAsync<ApiPositionV2[]>(
                    await result.Content.ReadAsStreamAsync(cancellationToken),
                    options,
                    cancellationToken: cancellationToken
                );

                var requestRouter = new RequestRouter(db);
                var tbnPositions = new List<TBNPosition>();

                foreach (var pos in positions)
                {
                    if (!departmentProjects.Contains(pos.ProjectId)) continue;

                    foreach (var instance in pos.Instances)
                    {
                        if (instance.AssignedPerson is not null) continue;

                        var department = await requestRouter.Route(pos, instance, cancellationToken);
                        if(department == request.Department)
                        {
                            tbnPositions.Add(new TBNPosition
                            {
                                PositionId = pos.Id,
                                InstanceId = instance.Id,
                                ParentPositionId = pos.ExternalId,
                                Project = pos.Project,
                                BasePosition = pos.BasePosition,
                                Name = pos.Name,

                                AppliesFrom = instance.AppliesFrom,
                                AppliesTo = instance.AppliesTo,

                                Workload = instance.Workload,
                                Obs = instance.Obs,

                                Department = department
                            });
                        }
                    }

                }

                return tbnPositions;
            }

        }
    }

    public class TBNPosition
    {
        public Guid PositionId { get; set; }
        public Guid InstanceId { get; set; }
        public string ParentPositionId { get; set; }

        public string Name { get; set; }
        public ApiProjectReferenceV2 Project { get; set; }
        public ApiPositionBasePositionV2 BasePosition { get; set; }

        public DateTime AppliesTo { get; set; }
        public DateTime AppliesFrom { get; set; }
        public string? Department { get; set; }
        public double? Workload { get; set; }
        public string Obs { get; set; }
    }
}
