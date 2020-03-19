using Fusion.AspNetCore.OData;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetContractPersonnel : IRequest<IEnumerable<QueryContractPersonnel>>
    {
        public GetContractPersonnel(Guid contractId, ODataQueryParams query = null)
        {
            ContractId = contractId;

            //ODataQueryParams IsEmpty() does not support OrderBy = null. Will fix in libraries-project, this can then be removed.
            Query = query ?? new ODataQueryParams { OrderBy = new List<ODataOrderByOption>() };
        }

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
                var itemQuery = db.ContractPersonnel.Where(p => p.Contract.OrgContractId == request.ContractId)
                    .AsQueryable();

                if (request.Query.HasFilter)
                    itemQuery = itemQuery.ApplyODataFilters(request.Query, m =>
                    {
                        m.MapField("azureAdStatus", p => p.Person.AccountStatus);
                        m.MapField("name", p => p.Person.Name);
                        m.MapField("phoneNumber", p => p.Person.Phone);
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
