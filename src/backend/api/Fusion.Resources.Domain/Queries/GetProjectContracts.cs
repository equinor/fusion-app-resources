using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain
{
    public class GetProjectContracts : IRequest<IEnumerable<QueryContract>>
    {

        public GetProjectContracts(Guid projectId)
        {
            ProjectId = projectId;
        }
        public Guid ProjectId { get; set; }


        public class Handler : IRequestHandler<GetProjectContracts, IEnumerable<QueryContract>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<IEnumerable<QueryContract>> Handle(GetProjectContracts request, CancellationToken cancellationToken)
            {
                var contracts = await db.Contracts.Where(c => c.ProjectId == request.ProjectId).ToListAsync();

                return contracts.Select(c => new QueryContract(c));
            }
        }
    }
}
