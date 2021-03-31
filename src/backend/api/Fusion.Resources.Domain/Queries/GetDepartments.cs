using Fusion.Integration;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
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

namespace Fusion.Resources.Domain
{
    public class GetDepartments : IRequest<IEnumerable<QueryDepartment>>
    {
        private bool shouldExpandResourceOwners = false;
        private string? resourceOwnerSearch;

        private string? departmentFilter;
        private string? sector;
        private string? departmentId;

        public IQueryable<QueryDepartment> Execute(IQueryable<DbDepartment> departments)
        {
            if(!string.IsNullOrEmpty(sector))
            {
                departments = departments.Where(dpt => dpt.SectorId == sector);
            }

            if(!string.IsNullOrEmpty(departmentFilter))
            {
                departments = departments.Where(dpt => dpt.DepartmentId.StartsWith(departmentFilter));
            }

            if (!string.IsNullOrEmpty(departmentId))
            {
                departments = departments.Where(dpt => dpt.DepartmentId == departmentId);
            }

            return departments.Select(dpt => new QueryDepartment(dpt));
        }

        public GetDepartments StartsWith(string department)
        {
            this.departmentFilter = department;
            return this;
        }

        public GetDepartments ById(string departmentId)
        {
            this.departmentId = departmentId;
            return this;
        }

        public GetDepartments InSector(string sector)
        {
            this.sector = sector;
            return this;
        }

        public GetDepartments ExpandResourceOwners()
        {
            shouldExpandResourceOwners = true;
            return this;
        }

        public GetDepartments WhereResourceOwnerMatches(string search)
        {
            resourceOwnerSearch = search;
            return this;
        }

        public class Handler : IRequestHandler<GetDepartments, IEnumerable<QueryDepartment>>
        {
            private readonly ResourcesDbContext db;
            private readonly IHttpClientFactory httpClientFactory;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ResourcesDbContext db, IHttpClientFactory httpClientFactory,
                IFusionProfileResolver profileResolver)
            { 
                this.db = db;
                this.httpClientFactory = httpClientFactory;
                this.profileResolver = profileResolver;
            }

            public async Task<IEnumerable<QueryDepartment>> Handle(GetDepartments request, CancellationToken cancellationToken)
            {
                var departments = await request.Execute(db.Departments).ToListAsync(cancellationToken);

                if(request.shouldExpandResourceOwners)
                {
                    return await ExpandResourceOwners(departments, request.resourceOwnerSearch, cancellationToken);
                }

                return departments;
            }

            private async Task<List<QueryDepartment>> ExpandResourceOwners(List<QueryDepartment> departments, string? filter, CancellationToken cancellationToken)
            {
                var result = new List<QueryDepartment>();
                var managedDepartments = departments.ToDictionary(dpt => dpt.DepartmentId);

                var client = httpClientFactory.CreateClient("lineorg");

                var uri = "/lineorg/persons?$filter=isresourceowner eq true";

                if (!string.IsNullOrEmpty(filter))
                    uri += $"&$search={filter}";

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

                        var department = managedDepartments[resourceOwner.FullDepartment];
                        department.LineOrgResponsible = await profileResolver.ResolvePersonBasicProfileAsync(resourceOwner.AzureUniqueId);

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
