using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Queries
{
    internal class GetRequestWorkflow : IRequest<DbWorkflow>
    {
        public GetRequestWorkflow(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public class Handler : IRequestHandler<GetRequestWorkflow, DbWorkflow>
        {
            private readonly ResourcesDbContext resourcesDb;

            public Handler(ResourcesDbContext resourcesDb)
            {
                this.resourcesDb = resourcesDb;
            }

            public async Task<DbWorkflow> Handle(GetRequestWorkflow request, CancellationToken cancellationToken)
            {
                var workflow = await resourcesDb.Workflows
                    .Include(wf => wf.WorkflowSteps).ThenInclude(s => s.CompletedBy)
                    .Include(wf => wf.TerminatedBy)
                    .FirstOrDefaultAsync(wf => wf.RequestId == request.RequestId);

                return workflow;
            }
        }
    }
}
