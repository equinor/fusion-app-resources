using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain
{
    public class GetProjects : IRequest<IEnumerable<QueryProject>>
    {


        public class Handler : IRequestHandler<GetProjects, IEnumerable<QueryProject>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<IEnumerable<QueryProject>> Handle(GetProjects request, CancellationToken cancellationToken)
            {
                var projects = await db.Projects.ToListAsync();

                return projects.Select(p => new QueryProject(p));
            }
        }
    }
}
