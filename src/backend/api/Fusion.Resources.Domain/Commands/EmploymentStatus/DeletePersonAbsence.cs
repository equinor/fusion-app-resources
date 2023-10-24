using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class DeletePersonAbsence : IRequest
    {
        public DeletePersonAbsence(PersonId personId, Guid id)
        {
            PersonId = personId;
            Id = id;
        }

        private Guid Id { get; set; }
        private PersonId PersonId { get; set; }


        public class Handler : IRequestHandler<DeletePersonAbsence>
        {
            private readonly ResourcesDbContext resourcesDb;

            public Handler(ResourcesDbContext resourcesDb)
            {
                this.resourcesDb = resourcesDb;
            }

            public async Task Handle(DeletePersonAbsence request, CancellationToken cancellationToken)
            {
                var dbEntity = await resourcesDb.PersonAbsences
                    .GetById(request.PersonId, request.Id)
                    .FirstOrDefaultAsync();

                if (dbEntity != null)
                {
                    resourcesDb.PersonAbsences.Remove(dbEntity);
                    await resourcesDb.SaveChangesAsync();
                }


            }
        }
    }
}
