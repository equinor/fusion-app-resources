using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetResponsibilityMatrixItem : IRequest<QueryResponsibilityMatrix?>
    {
        public GetResponsibilityMatrixItem(Guid id)
        {
            Id = id;
        }

        private Guid Id { get; set; }


        public class Handler : IRequestHandler<GetResponsibilityMatrixItem, QueryResponsibilityMatrix?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryResponsibilityMatrix?> Handle(GetResponsibilityMatrixItem request, CancellationToken cancellationToken)
            {
                var item = await db.ResponsibilityMatrices
                    .Include(x => x.CreatedBy)
                    .Include(x => x.Responsible)
                    .Include(x => x.Project)
                    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                return item != null ? new QueryResponsibilityMatrix(item) : null;
            }
        }
    }
}