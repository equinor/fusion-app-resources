using Fusion.Integration.Roles;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Departments
{
    public class AddDelegatedResourceOwner : IRequest
    {
        public AddDelegatedResourceOwner(string departmentId, Guid responsibleAzureUniqueId)
        {
            DepartmentId = departmentId;
            ResponsibleAzureUniqueId = responsibleAzureUniqueId;
        }
        public DateTimeOffset DateFrom { get; set; }
        public DateTimeOffset DateTo { get; set; }
        public string DepartmentId { get; }
        public Guid ResponsibleAzureUniqueId { get; }

        public class Handler : IRequestHandler<AddDelegatedResourceOwner>
        {
            const string ResourceOwnerRole = "Fusion.Resources.ResourceOwner";
            private readonly IFusionRolesClient rolesClient;

            public Handler(IFusionRolesClient rolesClient)
            {
                this.rolesClient = rolesClient;
            }

            public async Task<Unit> Handle(AddDelegatedResourceOwner request, CancellationToken cancellationToken)
            {
                var roleAssignment = await rolesClient.AssignRoleAsync(request.ResponsibleAzureUniqueId, new RoleAssignment
                {
                    Identifier = Guid.NewGuid().ToString(),
                    RoleName = Roles.ResourceOwner,
                    Scope = new RoleAssignment.RoleScope("OrgUnit", request.DepartmentId),
                    Source = "DelegatedResourceOwner",
                    ValidTo = request.DateTo
                });

                return Unit.Value;
            }
        }
    }
}
