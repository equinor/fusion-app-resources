using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
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
            private readonly ResourcesDbContext db;
            private readonly IFusionRolesClient rolesClient;

            public Handler(ResourcesDbContext db, IFusionRolesClient rolesClient)
            {
                this.db = db;
                this.rolesClient = rolesClient;
            }

            public async Task<Unit> Handle(AddDelegatedResourceOwner request, CancellationToken cancellationToken)
            {
                await rolesClient.AssignRoleAsync(request.ResponsibleAzureUniqueId, new RoleAssignment
                {
                    Identifier = Guid.NewGuid().ToString(),
                    RoleName = AccessRoles.ResourceOwner,
                    Scope = new RoleAssignment.RoleScope("OrgUnit", request.DepartmentId),
                    Source = "Fusion.Resources.DelegatedResourceOwner",
                    ValidTo = request.DateTo
                });

                return Unit.Value;
            }
        }
    }
}