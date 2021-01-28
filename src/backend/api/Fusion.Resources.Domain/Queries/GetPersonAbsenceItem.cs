using System;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetPersonAbsenceItem : IRequest<QueryEmploymentStatus>
    {
        private ODataQueryParams query = null!;

        public GetPersonAbsenceItem(PersonId personId, Guid id)
        {
            PersonId = personId;
            Id = id;
        }

        private PersonId PersonId { get; set; }
        private Guid Id { get; set; }


        public class Handler : IRequestHandler<GetPersonAbsenceItem, QueryEmploymentStatus>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryEmploymentStatus> Handle(GetPersonAbsenceItem request, CancellationToken cancellationToken)
            {
                var item = await db.PersonAbsences.GetById(request.PersonId, request.Id)
                    .Include(x => x.Person)
                    .Include(x => x.CreatedBy)
                    .FirstOrDefaultAsync();

                var returnItem = new QueryEmploymentStatus(item);
                return returnItem;
            }
        }
    }
}