using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Commands
{
    public class SyncProjectStates: IRequest
    {
        public class Handler : IRequestHandler<SyncProjectStates>
        {
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver)
            {
                this.db = db;
                this.orgResolver = orgResolver;
            }

            public async Task Handle(SyncProjectStates request, CancellationToken cancellationToken)
            {
                var projects = await db.Projects.ToListAsync(cancellationToken: cancellationToken);

                foreach (var project in projects)
                {
                    var orgProject = await orgResolver.ResolveProjectAsync(project.OrgProjectId);

                    if (orgProject is not null)
                    {
                        project.State = orgProject.State;
                    }
                }
                await db.SaveChangesAsync(cancellationToken);

            }
        }

    }
}

