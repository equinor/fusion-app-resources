using Fusion.Resources.Database;
using Fusion.Resources.Database.Migrations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
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

        /// <summary>
        /// Enable option to limit query to other tasks which is not marked private.
        /// </summary>
        public bool LimitToPublicAllocations { get; set; }
        public bool FilterPastAllocations { get; set; }

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

                if (request.LimitToPublicAllocations)
                {
                    items = items
                        // Only show other tasks that is not marked as private
                        .Where(a => a.Type == Database.Entities.DbAbsenceType.OtherTasks && a.IsPrivate == false)
                        .ToList();
                }

                var returnItems = items
                    // Remove past allocations
                    .Where(a => request.FilterPastAllocations == false || a.AppliesTo >= DateTime.Today)
                    .Select(i => new QueryPersonAbsence(i))
                    .ToList();

                return returnItems;
            }
        }
    }
}