using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetPersonAbsence : IRequest<IEnumerable<QueryPersonAbsence>>
    {
        public GetPersonAbsence(PersonId personId)
        {
            PersonId = personId;
        }

        private PersonId PersonId { get; set; }


        public class Handler : IRequestHandler<GetPersonAbsence, IEnumerable<QueryPersonAbsence>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryPersonAbsence>> Handle(GetPersonAbsence request, CancellationToken cancellationToken)
            {
                var items = await db.PersonAbsences.GetById(request.PersonId)
                    .Include(x => x.Person)
                    .Include(x => x.CreatedBy)
                    .Include(x => x.TaskDetails)
                    .ToListAsync(cancellationToken);

                var returnItems = items.Select(i => new QueryPersonAbsence(i))
                    .ToList();

                return returnItems;
            }
        }
    }
}