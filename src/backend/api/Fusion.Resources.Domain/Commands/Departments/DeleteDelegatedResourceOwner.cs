using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Departments
{
    public class DeleteDelegatedResourceOwner : IRequest<bool>
    {
        private readonly string departmentId;
        private readonly Guid delegatedOwnerAzureUniqueId;

        public DeleteDelegatedResourceOwner(string departmentId, Guid delegatedOwnerAzureUniqueId)
        {
            this.departmentId = departmentId;
            this.delegatedOwnerAzureUniqueId = delegatedOwnerAzureUniqueId;
        }

        public class Handler : IRequestHandler<DeleteDelegatedResourceOwner, bool>
        {
            private readonly ResourcesDbContext db;
            private readonly IFusionRolesClient rolesClient;

            public Handler(ResourcesDbContext db, IFusionRolesClient rolesClient)
            {
                this.db = db;
                this.rolesClient = rolesClient;
            }

            public async Task<bool> Handle(DeleteDelegatedResourceOwner request, CancellationToken cancellationToken)
            {
                var query = db.DepartmentResponsibles
                    .Where(x => x.DepartmentId == request.departmentId
                                && x.ResponsibleAzureObjectId == request.delegatedOwnerAzureUniqueId);
                db.DepartmentResponsibles.RemoveRange(query);

                var wasDeleted = await db.SaveChangesAsync(cancellationToken) > 0;

                if (!wasDeleted)
                    return false;

                var deleted = await rolesClient.DeleteRolesAsync(
                    new PersonIdentifier(request.delegatedOwnerAzureUniqueId),
                    q => q.WhereRoleName(AccessRoles.ResourceOwner).WhereScopeValue(request.departmentId)
                );

                return deleted.Any();
            }
        }
    }
}
