using Fusion.ApiClients.Org;
using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using Fusion.Resources.Domain.LineOrg;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetDepartmentWithResponsible : IRequest<QueryDepartmentWithResponsible>
    {
        public GetDepartmentWithResponsible(string departmentId)
        {
            DepartmentId = departmentId;
        }

        public string DepartmentId { get; }

        public class Handler : IRequestHandler<GetDepartmentWithResponsible, QueryDepartmentWithResponsible>
        {
            private readonly ResourcesDbContext db;
            private readonly IHttpClientFactory httpClientFactory;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ResourcesDbContext db, IHttpClientFactory httpClientFactory, IFusionProfileResolver profileResolver)
            {
                this.db = db;
                this.httpClientFactory = httpClientFactory;
                this.profileResolver = profileResolver;
            }

            public async Task<QueryDepartmentWithResponsible> Handle(GetDepartmentWithResponsible request, CancellationToken cancellationToken)
            {
                var department = await db.Departments.FindAsync(request.DepartmentId, cancellationToken);

                var client = httpClientFactory.CreateClient("lineorg");

                var uri = "/lineorg/persons/?$filter="
                    + $"isresourceowner eq true "
                    + $"and fulldepartment eq '{department.DepartmentId}'";

                var response = await client.GetAsync(uri);
                var lineOrgDpt = JsonSerializer.Deserialize<PaginatedResponse<ProfileWithDepartment>>(
                    await response.Content.ReadAsStringAsync(cancellationToken),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                var responsibleAzureId = lineOrgDpt?.Value.FirstOrDefault()?.AzureUniqueId;
                var responsible = responsibleAzureId.HasValue ? await profileResolver.ResolvePersonBasicProfileAsync(responsibleAzureId) : null;

                var responsibleOverride = await db.DepartmentResponsibles
                   .Where(r => r.DateFrom <= DateTime.Now && r.DateTo >= DateTime.Now)
                   .Where(r => r.DepartmentId == request.DepartmentId)
                   .FirstOrDefaultAsync(cancellationToken);

                var result = new QueryDepartmentWithResponsible(department, responsible);

                if (responsibleOverride is object)
                {
                    result.DefactoResponsible = await profileResolver.ResolvePersonBasicProfileAsync(responsibleOverride.ResponsibleAzureObjectId);
                }

                return result;
            }
        }
    }
}
