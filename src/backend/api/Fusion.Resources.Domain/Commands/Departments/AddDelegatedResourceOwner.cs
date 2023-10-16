using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Linq;
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
        public Guid? UpdatedByAzureUniqueId { get; set; }

        public string? Reason { get; set; }

        public AddDelegatedResourceOwner WithReason(string? reason)
        {
            Reason = reason;
            return this;
        }

        public class Handler : IRequestHandler<AddDelegatedResourceOwner>
        {
            private readonly ResourcesDbContext db;
            private readonly IFusionRolesClient rolesClient;

            public Handler(ResourcesDbContext db, IFusionRolesClient rolesClient)
            {
                this.db = db;
                this.rolesClient = rolesClient;
            }

            public async Task Handle(AddDelegatedResourceOwner request, CancellationToken cancellationToken)
            {
                var alreadyDelegated = db.DelegatedDepartmentResponsibles.Any(x =>
                    x.ResponsibleAzureObjectId == request.ResponsibleAzureUniqueId &&
                    x.DepartmentId == request.DepartmentId);

                if (alreadyDelegated)
                    throw new RoleDelegationExistsError();

                await rolesClient.AssignRoleAsync(request.ResponsibleAzureUniqueId, new RoleAssignment
                {
                    Identifier = Guid.NewGuid().ToString(),
                    RoleName = AccessRoles.ResourceOwner,
                    Scope = new RoleAssignment.RoleScope("OrgUnit", request.DepartmentId),
                    Source = "Fusion.Resources.DelegatedResourceOwner",
                    ValidTo = request.DateTo
                });

                var responsible = new DbDelegatedDepartmentResponsible
                {
                    DateCreated = DateTime.UtcNow,
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    DepartmentId = request.DepartmentId,
                    ResponsibleAzureObjectId = request.ResponsibleAzureUniqueId,
                    Reason = request.Reason,
                    UpdatedBy = request.UpdatedByAzureUniqueId
                };

                db.DelegatedDepartmentResponsibles.Add(responsible);
                await db.SaveChangesAsync();
            }
        }

    }
}