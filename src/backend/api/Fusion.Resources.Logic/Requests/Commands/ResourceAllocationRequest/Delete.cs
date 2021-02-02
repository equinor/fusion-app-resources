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
        public class Delete : TrackableRequest<bool>
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

            public class Handler : IRequestHandler<Delete, bool>
            {
                private readonly ResourcesDbContext dbContext;

                public Handler(ResourcesDbContext dbContext)
                {
                    this.dbContext = dbContext;
                }

                public async Task<bool> Handle(Delete request, CancellationToken cancellationToken)
                {
                    var req = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(c => c.Id == request.RequestId);

                    if (req != null)
                    {
                        dbContext.ResourceAllocationRequests.Remove(req);
                        await dbContext.SaveChangesAsync();
                        return true;
                    }
                    return false;

                }
            }
        }

    }
}