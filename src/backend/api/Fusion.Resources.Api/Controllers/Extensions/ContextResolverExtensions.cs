using System;
using System.Threading.Tasks;

namespace Fusion.Integration
{
    public static class ContextResolverExtensions
    {
        public static async Task<FusionContext> ResolveProjectMasterAsync(this IFusionContextResolver contextResolver, Resources.Api.Controllers.ProjectIdentifier identifier)
        {
            var context = await contextResolver.ResolveContextAsync(ContextIdentifier.FromExternalId(identifier.ProjectId), FusionContextType.OrgChart);

            if (context == null)
                throw new ProjectMasterNotFoundError(identifier);

            var projectMasterContext = await contextResolver.RelationsFirstOrDefaultAsync(context, FusionContextType.ProjectMaster);

            if (projectMasterContext == null)
                throw new ProjectMasterNotFoundError(identifier);

            return projectMasterContext;
        }

        public class ProjectMasterNotFoundError : Exception
        {
            public ProjectMasterNotFoundError(Resources.Api.Controllers.ProjectIdentifier projectIdentifier) :
                base($"Unable to find master project for '{projectIdentifier.Name} ({projectIdentifier.OriginalIdentifier})'")
            {
            }
        }
    }
}
