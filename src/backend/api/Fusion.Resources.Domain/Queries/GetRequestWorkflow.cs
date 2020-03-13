using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetRequestWorkflow : IRequest<QueryWorkflow>
    {
        public GetRequestWorkflow(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public class Handler : IRequestHandler<GetRequestWorkflow, QueryWorkflow>
        {
            private readonly ResourcesDbContext resourcesDb;

            public Handler(ResourcesDbContext resourcesDb)
            {
                this.resourcesDb = resourcesDb;
            }

            public async Task<QueryWorkflow> Handle(GetRequestWorkflow request, CancellationToken cancellationToken)
            {
                var workflow = await resourcesDb.Workflows
                    .Include(wf => wf.WorkflowSteps).ThenInclude(s => s.CompletedBy)
                    .Include(wf => wf.TerminatedBy)
                    .FirstOrDefaultAsync(wf => wf.RequestId == request.RequestId);

                return new QueryWorkflow(workflow);
            }
        }
    }
}

