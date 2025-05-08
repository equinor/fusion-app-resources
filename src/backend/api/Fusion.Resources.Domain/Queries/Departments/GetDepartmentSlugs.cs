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
    public class GetDepartmentSlugs : IRequest<IEnumerable<QueryDepartment>>
    {
        public class Handler : DepartmentHandlerBase, IRequestHandler<GetDepartmentSlugs, IEnumerable<QueryDepartment>>
        {
            private readonly ILineOrgClient lineorg;

            public Handler(
                ResourcesDbContext db, 
                ILineOrgClient lineorg, 
                IFusionProfileResolver profileResolver) : base(db, profileResolver)
            {
                this.lineorg = lineorg;
            }

            public async Task<IEnumerable<QueryDepartment>> Handle(GetDepartmentSlugs request, CancellationToken cancellationToken)
            {
                var orgUnits = await lineorg.LoadAllOrgUnitsAsync();

                var query = orgUnits.AsQueryable();

                var cmp = StringComparer.OrdinalIgnoreCase;

                List<QueryDepartment> result;

                result = query
                    .Select(o => new QueryDepartment(o))
                    .ToList();

                return result;
            }
        }
    }
}