using FluentValidation;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Commands;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class Delete : TrackableRequest
        {
            public Delete(Guid requestId)
            {
                RequestId = requestId;
            }

            private Guid RequestId { get; }

            public class Validator : AbstractValidator<Delete>
            {
                public Validator(ResourcesDbContext dbContext)
                {
                    RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                    {
                        return await dbContext.ResourceAllocationRequests.AnyAsync(r => r.Id == id);
                    }).WithMessage("Request id must exist");
                }
            }

            public class Handler : AsyncRequestHandler<Delete>
            {
                private readonly ResourcesDbContext dbContext;
                private readonly IMediator mediator;

                public Handler(ResourcesDbContext dbContext, IMediator mediator)
                {
                    this.dbContext = dbContext;
                    this.mediator = mediator;
                }

                protected override async Task Handle(Delete request,
                    CancellationToken cancellationToken)
                {
                    var req = await dbContext.ResourceAllocationRequests.FirstAsync(c => c.Id == request.RequestId);

                    if (req != null)
                        dbContext.ResourceAllocationRequests.Remove(req);
                    await dbContext.SaveChangesAsync();

                }
            }
        }

    }
}