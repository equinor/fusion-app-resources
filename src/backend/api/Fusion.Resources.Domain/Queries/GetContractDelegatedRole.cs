using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetContractDelegatedRole : IRequest<QueryDelegatedRole?>
    {
        public GetContractDelegatedRole(Guid roleId)
        {
            RoleId = roleId;
        }

        public Guid RoleId { get; }

        public class Handler : IRequestHandler<GetContractDelegatedRole, QueryDelegatedRole?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryDelegatedRole?> Handle(GetContractDelegatedRole request, CancellationToken cancellationToken)
            {
                var dbRole = await db.DelegatedRoles
                    .Include(r => r.Person)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.RecertifiedBy)
                    .Include(r => r.Project)
                    .Include(r => r.Contract)
                    .FirstOrDefaultAsync(r => r.Id == request.RoleId);

                if (dbRole != null)
                    return new QueryDelegatedRole(dbRole);

                return null;
            }


        }
    }

}