using Fusion.Resources.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    internal class DelegatedContractRoleAuthHandler : AuthorizationHandler<DelegatedContractRole, ContractResource>
    {
        private readonly ResourcesDbContext dbContext;

        public DelegatedContractRoleAuthHandler(ResourcesDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DelegatedContractRole requirement, ContractResource resource)
        {
            var userId = context.User.GetAzureUniqueId();

            // Cannot evaluate users without azure unique id.
            if (userId.HasValue == false)
                return;

            var roles = await dbContext.DelegatedRoles
                .Where(r => r.Person.AzureUniqueId == userId && r.Contract.OrgContractId == resource.Contract)                
                .ToListAsync();

            roles = roles.Where(r => r.ValidTo.UtcDateTime.Date >= DateTime.UtcNow.Date).ToList();

            // Filter roles based on classification before evaluating type.
            switch (requirement.Classification)
            {
                case DelegatedContractRole.RoleClassification.External:
                    roles = roles.Where(r => r.Classification == Database.Entities.DbDelegatedRoleClassification.External).ToList();
                    break;
                case DelegatedContractRole.RoleClassification.Internal:
                    roles = roles.Where(r => r.Classification == Database.Entities.DbDelegatedRoleClassification.Internal).ToList();
                    break;
                case null:
                    break;
                default:
                    throw new NotSupportedException("Classification not supported in delegated role evaluation.");
            }


            // Evaluate specific role type
            switch (requirement.Type)
            {
                case DelegatedContractRole.RoleType.Any:
                    if (roles.Any())
                        context.Succeed(requirement);
                    else
                        requirement.SetEvaluation("User does not have any delegated roles");
                    break;

                case DelegatedContractRole.RoleType.CompanyRep:
                    if (roles.Any(r => r.Type == Database.Entities.DbDelegatedRoleType.CR))
                        context.Succeed(requirement);
                    else
                        requirement.SetEvaluation(roles.Any() ? $"User have '{roles.Count}' roles, but does not have the CR role" : "User does not have any roles");
                    break;

                default:
                    requirement.SetEvaluation($"Specific role '{requirement.Type}' was not evaluated.");
                    break;
            }


        }
        
    }
}
