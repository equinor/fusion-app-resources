using Fusion.Resources.Database;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class DeleteResponsibilityMatrix : IRequest
    {
        public DeleteResponsibilityMatrix(Guid id)
        {
            Id = id;
        }

        private Guid Id { get; set; }

        public class Handler : AsyncRequestHandler<DeleteResponsibilityMatrix>
        {
            private readonly ResourcesDbContext resourcesDb;

            public Handler(ResourcesDbContext resourcesDb)
            {
                this.resourcesDb = resourcesDb;
            }

            protected override async Task Handle(DeleteResponsibilityMatrix request, CancellationToken cancellationToken)
            {
                resourcesDb.ResponsibilityMatrices.RemoveRange(
                    resourcesDb.ResponsibilityMatrices.Where(x => x.Id == request.Id)
                );
                await resourcesDb.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
