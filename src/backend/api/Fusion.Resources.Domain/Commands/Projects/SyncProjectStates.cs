using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Commands
{
    public class SyncProjectStates : TrackableRequest
    {
        public IEnumerable<Guid>? OrgProjectIds { get; private set; }

        public SyncProjectStates WhereOrgProjectIds(IEnumerable<Guid> orgProjectIds)
        {
            OrgProjectIds = orgProjectIds;
            return this;
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
                var projects = await db.Projects
                    .Where(p => request.OrgProjectIds == null || request.OrgProjectIds.Contains(p.OrgProjectId))
                    .ToListAsync(cancellationToken: cancellationToken);

                foreach (var project in projects)
                {
                    var orgProject = await orgResolver.ResolveProjectAsync(project.OrgProjectId);

                    if (orgProject is not null)
                    {
                        project.State = orgProject.State ?? "ACTIVE";
                    }
                }
                await db.SaveChangesAsync(cancellationToken);

            }
        }

    }
}

