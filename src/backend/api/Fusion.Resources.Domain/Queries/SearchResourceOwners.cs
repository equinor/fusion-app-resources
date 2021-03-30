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
    public class SearchResourceOwners : IRequest<List<QueryDepartmentWithResponsible>>
    {
        public SearchResourceOwners(string query)
        {
            Query = query;
        }

        public string Query { get; }

        public class Handler : IRequestHandler<SearchResourceOwners, List<QueryDepartmentWithResponsible>>
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

            public async Task<List<QueryDepartmentWithResponsible>> Handle(SearchResourceOwners request, CancellationToken cancellationToken)
            {
                var client = httpClientFactory.CreateClient("lineorg");

                var uri = "/lineorg/persons/?$filter=isresourceowner eq true"
                    + $"&$search={request.Query}";

                var managedDepartments = await db.Departments
                    .Where(dpt => dpt.DepartmentId != dpt.SectorId)
                    .ToDictionaryAsync(dpt => dpt.DepartmentId, cancellationToken);

                var response = await client.GetAsync(uri, cancellationToken);
                response.EnsureSuccessStatusCode();

                var resourceOwners = JsonSerializer.Deserialize<PaginatedResponse<ProfileWithDepartment>>(
                    await response.Content.ReadAsStringAsync(cancellationToken),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                )!.Value;

                var result = new List<QueryDepartmentWithResponsible>();
                foreach (var resourceOwner in resourceOwners)
                {
                    if (!managedDepartments.ContainsKey(resourceOwner.FullDepartment)) continue;

                    var responsible = await profileResolver.ResolvePersonBasicProfileAsync(resourceOwner.AzureUniqueId);
                    var department = new QueryDepartmentWithResponsible(managedDepartments[resourceOwner.FullDepartment], responsible);

                    var delegatedResourceOwner = await db.DepartmentResponsibles
                        .Where(r => r.DateFrom <= DateTime.UtcNow && r.DateTo >= DateTime.UtcNow)
                        .FirstOrDefaultAsync(r => r.DepartmentId == resourceOwner.FullDepartment, cancellationToken);

                    if (delegatedResourceOwner is not null)
                    {
                        department.DefactoResponsible = await profileResolver.ResolvePersonBasicProfileAsync(delegatedResourceOwner.ResponsibleAzureObjectId);
                    }

                    result.Add(department);
                }

                return result;
            }
        }
    }
}
