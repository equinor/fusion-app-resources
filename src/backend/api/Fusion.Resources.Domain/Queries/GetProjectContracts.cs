using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain
{    
    public class GetProjectContracts : IRequest<IEnumerable<QueryContract>>
    {
        private GetProjectContracts(Guid identifier, QueryType type)
        {
            QueryIdentifier = identifier;
            Type = type;
        }

        public Guid QueryIdentifier { get; set; }

        private QueryType Type { get; set; }

        private enum QueryType { ByInternalId, ByOrgId }

        public static GetProjectContracts ByInternalId(Guid projectId)
        {
            return new GetProjectContracts(projectId, QueryType.ByInternalId);
        }
        public static GetProjectContracts ByOrgProjectId(Guid projectOrgId)
        {
            return new GetProjectContracts(projectOrgId, QueryType.ByOrgId);
        }

        public class Handler : IRequestHandler<GetProjectContracts, IEnumerable<QueryContract>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryContract>> Handle(GetProjectContracts request, CancellationToken cancellationToken)
            {
                if (request.Type == QueryType.ByOrgId)
                {
                    var contracts = await db.Contracts.Where(c => c.Project.OrgProjectId == request.QueryIdentifier).ToListAsync();
                    return contracts.Select(c => new QueryContract(c));
                }
                else
                {
                    var contracts = await db.Contracts.Where(c => c.ProjectId == request.QueryIdentifier).ToListAsync();
                    return contracts.Select(c => new QueryContract(c));
                }
            }
        }
    }
}
