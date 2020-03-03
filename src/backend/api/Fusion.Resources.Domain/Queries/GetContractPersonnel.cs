using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain
{
    public class GetContractPersonnel : IRequest<IEnumerable<QueryContractPersonnel>>
    {
        public Guid ContractId { get; set; }
        public ODataQueryParams Query { get; set; }


        public class Handler : IRequestHandler<GetContractPersonnel, IEnumerable<QueryContractPersonnel>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryContractPersonnel>> Handle(GetContractPersonnel request, CancellationToken cancellationToken)
            {
                var itemQuery = db.ContractPersonnel.Where(p => p.ContractId == request.ContractId)                    
                    .AsQueryable();

                if (request.Query.HasFilter)
                    itemQuery = itemQuery.ApplyODataFilters(request.Query, m =>
                    {
                        m.MapField("accountStatus", p => p.Person.AccountStatus);
                        m.MapField("name", p => p.Person.Name);
                        m.MapField("phone", p => p.Person.Phone);
                        m.MapField("created", p => p.Created);
                    });

                var items = await itemQuery
                    .Include(i => i.Contract)
                    .Include(i => i.Project)
                    .Include(i => i.UpdatedBy)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Person).ThenInclude(p => p.Disciplines)
                    .ToListAsync();

                var returnItems = items.Select(i => new QueryContractPersonnel(i))
                    .ToList();

                return returnItems;
            }
        }
    }
}
