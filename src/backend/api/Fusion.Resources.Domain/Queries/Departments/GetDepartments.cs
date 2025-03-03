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

        private string? sector;
        private string[]? departmentIds = null;


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
            private readonly ILineOrgClient lineorg;

            public Handler(ResourcesDbContext db, ILineOrgClient lineorg, IFusionProfileResolver profileResolver)
                : base(db, profileResolver)
            {
                this.lineorg = lineorg;
            }

            public async Task<IEnumerable<QueryDepartment>> Handle(GetDepartments request, CancellationToken cancellationToken)
            {
                var orgUnits = await lineorg.LoadAllOrgUnitsAsync();

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

                    query = query.Where(d => items.Contains(d));
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
        }
    }
}