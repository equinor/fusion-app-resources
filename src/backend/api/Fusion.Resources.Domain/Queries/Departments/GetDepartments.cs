using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Application;
using Fusion.Resources.Database;
using Fusion.Services.LineOrg.ApiModels;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetDepartments : IRequest<IEnumerable<QueryDepartment>>
    {
        private bool shouldExpandDelegatedResourceOwners = false;
        private string? resourceOwnerSearch;

        private string? departmentIdStartsWith;
        private string? sector;
        private string[]? departmentIds = null;

        public GetDepartments StartsWith(string department)
        {
            this.departmentIdStartsWith = department;
            return this;
        }

        public GetDepartments ByIds(params string[] departmentIds)
        {
            this.departmentIds = departmentIds;
            return this;
        }

        public GetDepartments ByIds(IEnumerable<string> departmentIds)
        {
            this.departmentIds = departmentIds.ToArray();
            return this;
        }

        public GetDepartments InSector(string sector)
        {
            this.sector = sector;
            return this;
        }

        public GetDepartments ExpandDelegatedResourceOwners()
        {
            shouldExpandDelegatedResourceOwners = true;
            return this;
        }

        public GetDepartments WhereResourceOwnerMatches(string search)
        {
            resourceOwnerSearch = search;
            return this;
        }

        public class Handler : DepartmentHandlerBase, IRequestHandler<GetDepartments, IEnumerable<QueryDepartment>>
        {
            private readonly IHttpClientFactory httpClientFactory;

            public Handler(ResourcesDbContext db, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileResolver, IHttpClientFactory httpClientFactory)
                : base(db, lineOrgResolver, profileResolver)
            {
                this.httpClientFactory = httpClientFactory;
            }

            public async Task<IEnumerable<QueryDepartment>> Handle(GetDepartments request, CancellationToken cancellationToken)
            {
                List<QueryDepartment> result;

                IEnumerable<ApiLineOrgUser> lineOrgDepartments;
                if (!string.IsNullOrEmpty(request.resourceOwnerSearch))
                    lineOrgDepartments = await lineOrgResolver.ResolveResourceOwnersAsync(request.resourceOwnerSearch);
                else
                    lineOrgDepartments = await lineOrgResolver.ResolveResourceOwnersAsync();

                if (request.departmentIds is not null)
                {
                    var ids = new HashSet<string>(request.departmentIds);
                    lineOrgDepartments = lineOrgDepartments
                        .Where(x => ids.Contains(x.FullDepartment!))
                        .ToList();
                }

                if (!string.IsNullOrEmpty(request.sector))
                {
                    lineOrgDepartments = lineOrgDepartments
                        .Where(x => new DepartmentPath(x.FullDepartment!).Parent() == request.sector)
                        .ToList();
                }

                if (!string.IsNullOrEmpty(request.departmentIdStartsWith))
                {
                    lineOrgDepartments = lineOrgDepartments
                        .Where(x => new DepartmentPath(x.FullDepartment!).Parent() == request.sector)
                        .ToList();
                }

                result = await lineOrgDepartments.ToQueryDepartment(profileResolver);

                if (!string.IsNullOrEmpty(request.resourceOwnerSearch))
                {
                    result = result.Where(dpt =>
                        dpt.DepartmentId.Contains(request.resourceOwnerSearch, StringComparison.OrdinalIgnoreCase)
                        || dpt.LineOrgResponsible?.Name.Contains(request.resourceOwnerSearch, StringComparison.OrdinalIgnoreCase) == true
                        || dpt.LineOrgResponsible?.Mail?.Contains(request.resourceOwnerSearch, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                if (request.shouldExpandDelegatedResourceOwners)
                {
                    await ExpandDelegatedResourceOwner(result, cancellationToken);
                }

                return result;
            }
       
        }
    }

    public class GetDepartmentsV2 : IRequest<IEnumerable<QueryDepartment>>
    {
        private bool shouldExpandDelegatedResourceOwners = false;
        private string? resourceOwnerSearch;

        private string? departmentIdStartsWith;
        private string? sector;
        private string[]? departmentIds = null;

        public GetDepartmentsV2 StartsWith(string department)
        {
            this.departmentIdStartsWith = department;
            return this;
        }

        public GetDepartmentsV2 ByIds(params string[] departmentIds)
        {
            this.departmentIds = departmentIds;
            return this;
        }

        public GetDepartmentsV2 ByIds(IEnumerable<string> departmentIds)
        {
            this.departmentIds = departmentIds.ToArray();
            return this;
        }

        public GetDepartmentsV2 InSector(string sector)
        {
            this.sector = sector;
            return this;
        }

        public GetDepartmentsV2 ExpandDelegatedResourceOwners()
        {
            shouldExpandDelegatedResourceOwners = true;
            return this;
        }

        public GetDepartmentsV2 WhereResourceOwnerMatches(string search)
        {
            resourceOwnerSearch = search;
            return this;
        }


        public class Handler : DepartmentHandlerBase, IRequestHandler<GetDepartmentsV2, IEnumerable<QueryDepartment>>
        {
            private readonly IHttpClientFactory httpClientFactory;
            private readonly IMemoryCache cache;

            public Handler(ResourcesDbContext db, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileResolver, IHttpClientFactory httpClientFactory, IMemoryCache cache)
                : base(db, lineOrgResolver, profileResolver)
            {
                this.httpClientFactory = httpClientFactory;
                this.cache = cache;
            }

            public async Task<IEnumerable<QueryDepartment>> Handle(GetDepartmentsV2 request, CancellationToken cancellationToken)
            {

                var orgUnits = await LoadLineOrgUnitsAsync();


                var query = orgUnits.AsQueryable();

                var cmp = StringComparer.OrdinalIgnoreCase;

                if (!string.IsNullOrEmpty(request.resourceOwnerSearch))
                {
                    query = query.Where(o => o.FullDepartment.ContainsNullSafe(request.resourceOwnerSearch)
                        || (o.Management != null && o.Management.Persons.Any(p => p.Name.ContainsNullSafe(request.resourceOwnerSearch) || p.Mail.ContainsNullSafe(request.resourceOwnerSearch))
                    ));
                }


                if (request.departmentIds?.Any() == true)
                {
                    var ids = new HashSet<string>(request.departmentIds);

                    query = query.Where(o => ids.Contains(o.FullDepartment));
                }

                if (!string.IsNullOrEmpty(request.sector))
                {
                    var items = query.ToList()
                       .Where(o => new DepartmentPath(o.FullDepartment!).Parent() == request.sector)
                       .ToList();

                    query = query.AsQueryable();
                }

                if (!string.IsNullOrEmpty(request.departmentIdStartsWith))
                {
                    var items = query.ToList()
                        .Where(x => new DepartmentPath(x.FullDepartment!).Parent() == request.sector)
                        .ToList();

                    query = query.AsQueryable();
                }

                List<QueryDepartment> result;

                result = query.Select(o => new QueryDepartment(o))
                    .ToList();

                if (request.shouldExpandDelegatedResourceOwners)
                {
                    await ExpandDelegatedResourceOwner(result, cancellationToken);
                }

                return result;
            }


            private async Task<List<ApiOrgUnit>> LoadLineOrgUnitsAsync()
            {
                if (cache.TryGetValue<List<ApiOrgUnit>>("line-org-units", out var cachedItems))
                    return cachedItems!;

                var client = httpClientFactory.CreateClient(Fusion.Integration.IntegrationConfig.HttpClients.ApplicationLineOrg());
                var resp = await client.GetAsync("/org-units?$top=5000&$expand=management");

                var json = await resp.Content.ReadAsStringAsync();
                var orgUnits = JsonConvert.DeserializeAnonymousType(json, new { value = new List<ApiOrgUnit>() });

                cache.Set("line-org-units", orgUnits.value, TimeSpan.FromMinutes(5));

                return orgUnits.value;

            }
        }
    }

    public static class CompareUtils
    {
        public static bool ContainsNullSafe(this string? value, string? comp)
        {
            return (value ?? "").Contains(comp ?? "", StringComparison.OrdinalIgnoreCase);
        }
    }
}