using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetResponsibilityMatrices : IRequest<IEnumerable<QueryResponsibilityMatrix>>
    {
        public class Handler : IRequestHandler<GetResponsibilityMatrices, IEnumerable<QueryResponsibilityMatrix>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryResponsibilityMatrix>> Handle(GetResponsibilityMatrices request, CancellationToken cancellationToken)
            {
                var items = await db.ResponsibilityMatrices
                    .Include(x => x.CreatedBy)
                    .Include(x => x.Project)
                    .Include(x => x.Responsible)
                    .ToListAsync(cancellationToken);

                return items.Select(i => new QueryResponsibilityMatrix(i)).ToList();
            }
        }
    }
}