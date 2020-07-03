using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Integration.Org;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetContractDelegatedRoles : IRequest<IEnumerable<QueryDelegatedRole>>
    {
        private GetContractDelegatedRoles(Guid orgProjectId, Guid? orgContractId = null)
        {
            OrgProjectId = orgProjectId;
            OrgContractId = orgContractId;
        }

        public Guid OrgProjectId { get; set; }
        public Guid? OrgContractId { get; set; }

        public static GetContractDelegatedRoles ForProject(Guid orgProjectId)
        {
            return new GetContractDelegatedRoles(orgProjectId);
        }

        public static GetContractDelegatedRoles ForContract(Guid orgProjectId, Guid contractId)
        {
            return new GetContractDelegatedRoles(orgProjectId, contractId);
        }

        public class Handler : IRequestHandler<GetContractDelegatedRoles, IEnumerable<QueryDelegatedRole>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryDelegatedRole>> Handle(GetContractDelegatedRoles request, CancellationToken cancellationToken)
            {
                var dbRolesQuery = db.DelegatedRoles
                    .Include(r => r.Person)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.RecertifiedBy)
                    .Include(r => r.Project)
                    .Include(r => r.Contract)
                    .Where(p => p.Project.OrgProjectId == request.OrgProjectId)
                    .AsQueryable();

                if (request.OrgContractId.HasValue)
                    dbRolesQuery = dbRolesQuery.Where(r => r.Contract.OrgContractId == request.OrgContractId.Value);

                var dbRoles = await dbRolesQuery.ToListAsync();

                var roles = dbRoles.Select(i => new QueryDelegatedRole(i))
                    .ToList();
       
                return roles;
            }

         
        }
    }

}