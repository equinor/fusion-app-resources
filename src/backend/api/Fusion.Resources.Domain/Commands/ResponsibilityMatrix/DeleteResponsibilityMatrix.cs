using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
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


        public class Handler : IRequestHandler<DeleteResponsibilityMatrix>
        {
            private readonly ResourcesDbContext resourcesDb;

            public Handler(ResourcesDbContext resourcesDb)
            {
                this.resourcesDb = resourcesDb;
            }

            public async Task Handle(DeleteResponsibilityMatrix request, CancellationToken cancellationToken)
            {
                var dbEntity = await resourcesDb.ResponsibilityMatrices
                    .FirstOrDefaultAsync(x=>x.Id==request.Id);

                if (dbEntity != null)
                {
                    resourcesDb.ResponsibilityMatrices.Remove(dbEntity);
                    await resourcesDb.SaveChangesAsync();
                }


            }
        }
    }
}
