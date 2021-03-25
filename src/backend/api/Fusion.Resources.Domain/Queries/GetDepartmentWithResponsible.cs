using Fusion.ApiClients.Org;
using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
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
                var result = new List<QueryDepartmentWithResponsible>();

                var department = await db.Departments.FindAsync(request.DepartmentId);
                var responsibleOverride = await db.DepartmentResponsibles
                    .Where(r => r.DateFrom <= DateTime.Now && r.DateTo >= DateTime.Now)
                    .Where(r => r.DepartmentId == request.DepartmentId)
                    .FirstOrDefaultAsync();


                Guid? responsibleAzureId;
                if (responsibleOverride is object)
                {
                    responsibleAzureId = responsibleOverride.ResponsibleAzureObjectId;
                }
                else
                {

                    var client = httpClientFactory.CreateClient("lineorg");

                    var uri = "/lineorg/persons/?$filter="
                        + "isresourceowner eq true "
                        + $"and fulldepartment eq '{department.DepartmentId}'";

                    var response = await client.GetAsync(uri);
                    var lineOrgDpt = JsonSerializer.Deserialize<PaginatedResponse>(
                        await response.Content.ReadAsStringAsync(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    responsibleAzureId = lineOrgDpt?.Value.FirstOrDefault()?.AzureUniqueId;
                }
                var responsible = responsibleAzureId.HasValue ? await profileResolver.ResolvePersonBasicProfileAsync(responsibleAzureId) : null;

                return new QueryDepartmentWithResponsible(department, responsible);
            }
        }
    }


    class PaginatedResponse
    {
        public int TotalCount { get; set; }
        public int Count { get; set; }
        public List<Profile> Value { get; set; }
    }
    class Profile
    {
        public Guid AzureUniqueId { get; set; }
    }
}
