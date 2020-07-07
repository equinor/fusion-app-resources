using FluentValidation;
using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class RecertifyRoleDelegation : TrackableRequest<QueryDelegatedRole>
    {
        public RecertifyRoleDelegation(Guid roleId, DateTimeOffset newDate)
        {
            RoleId = roleId;
            NewValidToDate = newDate;
        }

        public Guid RoleId { get; }
        public DateTimeOffset NewValidToDate { get; }

        public class Validator : AbstractValidator<RecertifyRoleDelegation>
        {
            public Validator(ResourcesDbContext dbContext)
            {
                RuleFor(x => x.NewValidToDate).Must(x => x < DateTime.UtcNow.AddYears(1)).WithMessage("Cannot extend permission for longer than 1 year");
                RuleFor(x => x.RoleId).MustAsync(async (id, cancel) =>
                {
                    return await dbContext.DelegatedRoles.AnyAsync(r => r.Id == id);
                }).WithMessage("Role id must exist");
            }
        }

        public class Handler : IRequestHandler<RecertifyRoleDelegation, QueryDelegatedRole>
        {
            private readonly ResourcesDbContext dbContext;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext dbContext, IMediator mediator)
            {
                this.dbContext = dbContext;
                this.mediator = mediator;
            }

            public async Task<QueryDelegatedRole> Handle(RecertifyRoleDelegation request, CancellationToken cancellationToken)
            {
                var role = await dbContext.DelegatedRoles
                    .Include(r => r.Person)
                    .Include(r => r.Contract)
                    .FirstAsync(c => c.Id == request.RoleId);

                role.ValidTo = request.NewValidToDate;
                role.RecertifiedDate = DateTime.UtcNow;
                role.RecertifiedBy = request.Editor.Person;

                await dbContext.SaveChangesAsync();

                // Update assignment
                await mediator.Publish(new Notifications.RecertifyContractReadRoleAssignment(role.Id));

                return new QueryDelegatedRole(role);
            }
        }
    }

}
