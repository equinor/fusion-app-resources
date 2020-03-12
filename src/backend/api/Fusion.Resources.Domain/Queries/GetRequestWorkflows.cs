using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetRequestWorkflows : IRequest<IEnumerable<QueryWorkflow>>
    {
        public GetRequestWorkflows(IEnumerable<Guid> requestIds)
        {
            var ids = requestIds.ToList();
            if (ids.Count == 0)
                throw new ArgumentException("At least one request id has to be provided");

            RequestIds = ids;
        }

        public IEnumerable<Guid> RequestIds { get; }

        public class Handler : IRequestHandler<GetRequestWorkflows, IEnumerable<QueryWorkflow>>
        {
            private readonly ResourcesDbContext resourcesDb;

            public Handler(ResourcesDbContext resourcesDb)
            {
                this.resourcesDb = resourcesDb;
            }

            public async Task<IEnumerable<QueryWorkflow>> Handle(GetRequestWorkflows request, CancellationToken cancellationToken)
            {
                var ids = request.RequestIds.ToList();

                var workflows = await resourcesDb.Workflows
                    .Include(wf => wf.WorkflowSteps).ThenInclude(s => s.CompletedBy)
                    .Include(wf => wf.TerminatedBy)
                    .Where(wf => ids.Contains(wf.RequestId))
                    .ToListAsync();

                return workflows.Select(wf => new QueryWorkflow(wf));
            }
        }
    }
}

