using FluentValidation;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class DeleteRoleDelegation : TrackableRequest
    {
        public DeleteRoleDelegation(Guid roleId)
        {
            RoleId = roleId;
        }

        public Guid RoleId { get; }

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

        public class Handler : AsyncRequestHandler<DeleteRoleDelegation>
        {
            private readonly ResourcesDbContext dbContext;

            public Handler(ResourcesDbContext dbContext)
            {
                this.dbContext = dbContext;
            }

            protected override async Task Handle(DeleteRoleDelegation request, CancellationToken cancellationToken)
            {
                var role = await dbContext.DelegatedRoles.FirstAsync(c => c.Id == request.RoleId);

                dbContext.DelegatedRoles.Remove(role);
                await dbContext.SaveChangesAsync();
            }
        }
    }

}
