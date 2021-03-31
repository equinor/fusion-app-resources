using Fusion.Integration;
using Fusion.Resources.Database;
using Fusion.Resources.Domain.LineOrg;
using Fusion.Resources.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class SearchDepartments : IRequest<List<QueryDepartment>>
    {
        public SearchDepartments(string query)
        {
            Query = query;
        }

        public string Query { get; }

        public class Handler : IRequestHandler<SearchDepartments, List<QueryDepartment>>
        {
            private readonly ResourcesDbContext db;
            private readonly IHttpClientFactory httpClientFactory;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ResourcesDbContext db,
                IHttpClientFactory httpClientFactory,
                IFusionProfileResolver profileResolver)
            {
                this.db = db;
                this.httpClientFactory = httpClientFactory;
                this.profileResolver = profileResolver;
            }

            public async Task<List<QueryDepartment>> Handle(SearchDepartments request, CancellationToken cancellationToken)
            {
                var result = new List<QueryDepartment>();

                var managedDepartments = await db.Departments
                    .Where(dpt => dpt.DepartmentId != dpt.SectorId)
                    .ToDictionaryAsync(dpt => dpt.DepartmentId, cancellationToken);

                var client = httpClientFactory.CreateClient("lineorg");

                var uri = "/lineorg/persons?$filter=isresourceowner eq true"
                    + $"&$search={request.Query}";

                do
                {
                    var response = await client.GetAsync(uri, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var page = JsonSerializer.Deserialize<PaginatedResponse<ProfileWithDepartment>>(
                        await response.Content.ReadAsStringAsync(cancellationToken),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    uri = page?.NextPage;
                    var resourceOwners = page!.Value;

                    foreach (var resourceOwner in resourceOwners)
                    {
                        if (!managedDepartments.ContainsKey(resourceOwner.FullDepartment)) continue;

                        var responsible = await profileResolver.ResolvePersonBasicProfileAsync(resourceOwner.AzureUniqueId);
                        var department = new QueryDepartment(managedDepartments[resourceOwner.FullDepartment], responsible);

                        var delegatedResourceOwner = await db.DepartmentResponsibles
                            .Where(r => r.DateFrom <= DateTime.UtcNow && r.DateTo >= DateTime.UtcNow)
                            .FirstOrDefaultAsync(r => r.DepartmentId == resourceOwner.FullDepartment, cancellationToken);

                        if (delegatedResourceOwner is not null)
                        {
                            department.DefactoResponsible = await profileResolver.ResolvePersonBasicProfileAsync(delegatedResourceOwner.ResponsibleAzureObjectId);
                        }

                        result.Add(department);
                    }
                } while (!string.IsNullOrEmpty(uri));

                return result;
            }
        }
    }
}
