using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetExternalPersonnelPerson : IRequest<QueryExternalPersonnelPerson>
    {
        public GetExternalPersonnelPerson(PersonnelId personnelId)
        {
            PersonnelId = personnelId;
        }

        public PersonnelId PersonnelId { get; }

        public class Handler : IRequestHandler<GetExternalPersonnelPerson, QueryExternalPersonnelPerson>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryExternalPersonnelPerson> Handle(GetExternalPersonnelPerson request, CancellationToken cancellationToken)
            {
                var person = await db.ExternalPersonnel
                    .Include(ep => ep.Disciplines)
                    .GetById(request.PersonnelId)
                    .FirstOrDefaultAsync();

                return new QueryExternalPersonnelPerson(person);
            }
        }
    }
}
