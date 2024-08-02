using Fusion.Resources.Database;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fusion.Integration.Org;


namespace Fusion.Resources.Domain.Commands
{
    public class SyncProjectStates: IRequest
    {



        public SyncProjectStates()
        {
            
        }


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
                var projects = await db.Projects.ToListAsync();

                foreach (var project in projects)
                {
                    var orgProject = await orgResolver.ResolveProjectAsync(project.OrgProjectId);

                    if (orgProject is not null)
                    {
                        var state = orgProject.State;
                        project.State = state;
                    }
                }
                await db.SaveChangesAsync(cancellationToken);

            }
        }

    }
}

