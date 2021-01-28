using Fusion.AspNetCore.OData;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetPersonAbsence : IRequest<IEnumerable<QueryEmploymentStatus>>
    {
        private ODataQueryParams query = null!;

        public GetPersonAbsence(PersonId personId)
        {
            PersonId = personId;
        }

        private PersonId PersonId { get; set; }


        public class Handler : IRequestHandler<GetPersonAbsence, IEnumerable<QueryEmploymentStatus>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryEmploymentStatus>> Handle(GetPersonAbsence request, CancellationToken cancellationToken)
            {
                var items = await db.PersonAbsences.GetById(request.PersonId)
                    .Include(x => x.Person)
                    .Include(x => x.CreatedBy)
                    .ToListAsync();



                var returnItems = items.Select(i => new QueryEmploymentStatus(i))
                    .ToList();

                return returnItems;
            }
        }
    }
}