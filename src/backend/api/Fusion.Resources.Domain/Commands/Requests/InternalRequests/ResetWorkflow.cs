using Fusion.Resources.Database;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Fusion.Resources.Database.Entities.DbResourceAllocationRequest;

namespace Fusion.Resources.Domain.Commands
{
    public class ResetWorkflow : IRequest<bool>
    {
        private readonly Guid requestId;
        public ResetWorkflow(Guid requestId)
        {
            this.requestId = requestId;
        }

        public class Handler : IRequestHandler<ResetWorkflow, bool>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<bool> Handle(ResetWorkflow request, CancellationToken cancellationToken)
            {
                var requestItem = await db.ResourceAllocationRequests
                    .FindAsync(new object[] { request.requestId }, cancellationToken);

                if (requestItem is null)
                    return false;

                requestItem.State = new DbOpState();
                requestItem.IsDraft = true;
                requestItem.AssignedDepartment = null;
                requestItem.ProposedPerson = new DbOpProposedPerson();

                db.Workflows.RemoveRange(
                    db.Workflows.Where(wf => wf.RequestId == request.requestId)
                );

                return 0 < await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
