using Fusion.Resources.Api.Authorization.Requirements;
using Fusion.Resources.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    public class RequestCreatorHandler : AuthorizationHandler<RequestCreatorRequirement>
    {
        private readonly ResourcesDbContext db;

        public RequestCreatorHandler(ResourcesDbContext db)
        {
            this.db = db;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RequestCreatorRequirement requirement)
        {
            var userId = context.User.GetAzureUniqueIdOrThrow();
            var request = await db.ResourceAllocationRequests
                    .Include(req => req.CreatedBy)
                    .SingleAsync(req => req.Id == requirement.RequestId);

            if(request.CreatedBy.AzureUniqueId == userId)
            {
                context.Succeed(requirement);
            }
        }
    }
}
