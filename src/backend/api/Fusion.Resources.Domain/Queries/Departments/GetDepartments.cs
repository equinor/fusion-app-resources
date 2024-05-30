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
            private readonly IMemoryCache cache;

            public const string OrgUnitsMemCacheKey = "line-org-org-units";

            public Handler(ResourcesDbContext db, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileResolver, IHttpClientFactory httpClientFactory, IMemoryCache cache)
                : base(db, lineOrgResolver, profileResolver)
            {
                this.httpClientFactory = httpClientFactory;
                this.cache = cache;
            }

            public async Task<IEnumerable<QueryDepartment>> Handle(GetDepartments request, CancellationToken cancellationToken)
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

            /// <summary>
            /// Quick fix for now. Should wrap line org functionality in seperate service that centralized this a bit more.
            /// Could also consider gathering requirements here and update the integration lib.. 
            /// 
            /// Cache is invalidated on updates from line-org by event handler <see cref="LineOrgOrgUnitHandler"/>
            /// </summary>
            private async Task<List<ApiOrgUnit>> LoadLineOrgUnitsAsync()
            {
                if (cache.TryGetValue<List<ApiOrgUnit>>(OrgUnitsMemCacheKey, out var cachedItems))
                    return cachedItems!;

                var client = httpClientFactory.CreateClient(Fusion.Integration.IntegrationConfig.HttpClients.ApplicationLineOrg());
                var resp = await client.GetAsync("/org-units?$top=5000&$expand=management");

                var json = await resp.Content.ReadAsStringAsync();
                var orgUnits = JsonConvert.DeserializeAnonymousType(json, new { value = new List<ApiOrgUnit>() });

                cache.Set(OrgUnitsMemCacheKey, orgUnits.value, TimeSpan.FromMinutes(60));

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