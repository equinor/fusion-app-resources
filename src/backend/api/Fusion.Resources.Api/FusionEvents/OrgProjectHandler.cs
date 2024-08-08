using Fusion.Events;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api
{
    public class OrgProjectHandler : ISubscriptionHandler
    {
        private readonly ResourcesDbContext db;
        private readonly IProjectOrgResolver orgResolver;
        private readonly ILogger<OrgProjectHandler> logger;

        public OrgProjectHandler(ResourcesDbContext db, IProjectOrgResolver orgResolver, ILogger<OrgProjectHandler> logger)
        {
            this.db = db;
            this.orgResolver = orgResolver;
            this.logger = logger;
        }

        public async Task ProcessMessageAsync(MessageContext ctx, string? body, CancellationToken cancellationToken)
        {
            var payload = ctx.GetBody<Events.Org.OrgSubscriptionEvent>();

            if (payload is null)
            {
                logger.LogCritical($"Received project update from org service, but failed to parse body ```{body}```");
                return;
            }

            switch (payload.Type)
            {
                case Events.Org.OrgSubscriptionEventType.ProjectUpdated:
                    await SynchronizeProjectInfo(payload, cancellationToken);
                    break;
                default:
                    logger.LogInformation($"Ignored event {payload.Type} for item id '{payload.ItemId}'");
                    break;
            }
        }

        private async Task SynchronizeProjectInfo(Events.Org.OrgSubscriptionEvent payload, CancellationToken cancellationToken)
        {
            var orgProject = await orgResolver.ResolveProjectAsync(payload.ItemId);

            if (orgProject is null)
            {
                logger.LogCritical($"Received project update from org service, but failed to resolve project with id '{payload?.ProjectId}'");
                return;
            }

            var existingProject = await db.Projects.FirstOrDefaultAsync(p => p.OrgProjectId == orgProject.ProjectId, cancellationToken: cancellationToken);

            // Only update tracked projects.
            if (existingProject is not null)
            {
                existingProject.Name = orgProject.Name;
                existingProject.DomainId = orgProject.DomainId;
                existingProject.State = orgProject.State;

                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}