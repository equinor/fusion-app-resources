using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Departments
{
    public  class DeleteDelegatedResourceOwner : IRequest<bool>
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
            private readonly IFusionRolesClient rolesClient;

            public Handler(IFusionRolesClient rolesClient)
            {
                this.rolesClient = rolesClient;
            }

            public async Task<bool> Handle(DeleteDelegatedResourceOwner request, CancellationToken cancellationToken)
            {
                var deleted = await rolesClient.DeleteRolesAsync(
                    new PersonIdentifier(request.delegatedOwnerAzureUniqueId),
                    q => q.WhereRoleName(Roles.ResourceOwner).WhereScopeValue(request.departmentId)
                );

                return deleted.Count() > 0;
            }
        }
    }
}
