using FluentValidation;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class DeleteProjectAllocationRequestCommand : TrackableRequest
    {
        public DeleteProjectAllocationRequestCommand(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public class Validator : AbstractValidator<DeleteProjectAllocationRequestCommand>
        {
            public Validator(ResourcesDbContext dbContext)
            {
                RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                {
                    return await dbContext.ResourceAllocationRequests.AnyAsync(r => r.Id == id);
                }).WithMessage("Request id must exist");
            }
        }

        public class Handler : AsyncRequestHandler<DeleteProjectAllocationRequestCommand>
        {
            private readonly ResourcesDbContext dbContext;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext dbContext, IMediator mediator)
            {
                this.dbContext = dbContext;
                this.mediator = mediator;
            }

            protected override async Task Handle(DeleteProjectAllocationRequestCommand request, CancellationToken cancellationToken)
            {
                var req = await dbContext.ResourceAllocationRequests.FirstAsync(c => c.Id == request.RequestId);

                dbContext.ResourceAllocationRequests.Remove(req);
                await dbContext.SaveChangesAsync();

            }
        }
    }

}
