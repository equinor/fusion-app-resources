using FluentValidation;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class DeleteProjectAllocationRequest : TrackableRequest
    {
        public DeleteProjectAllocationRequest(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public class Validator : AbstractValidator<DeleteProjectAllocationRequest>
        {
            public Validator(ResourcesDbContext dbContext)
            {
                /*RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                {
                    return await dbContext.AllocationRequest.AnyAsync(r => r.Id == id);
                }).WithMessage("Request id must exist");*/
            }
        }

        public class Handler : AsyncRequestHandler<DeleteProjectAllocationRequest>
        {
            private readonly ResourcesDbContext dbContext;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext dbContext, IMediator mediator)
            {
                this.dbContext = dbContext;
                this.mediator = mediator;
            }

            protected override async Task Handle(DeleteProjectAllocationRequest request, CancellationToken cancellationToken)
            {
                /*var req = await dbContext.AllocationRequest.FirstAsync(c => c.Id == request.RequestId);

                dbContext.AllocationRequest.Remove(req);
                await dbContext.SaveChangesAsync();*/

            }
        }
    }

}
