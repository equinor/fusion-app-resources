using Fusion.Integration;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{

    public class GetPersonNotes : IRequest<IEnumerable<QueryPersonNote>>
    {
        public GetPersonNotes(Guid azureUniqueId)
        {
            AzureUniqueId = azureUniqueId;
        }

        public Guid AzureUniqueId { get; }

        public class Handler : IRequestHandler<GetPersonNotes, IEnumerable<QueryPersonNote>>
        {
            private readonly ResourcesDbContext dbContext;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ResourcesDbContext dbContext, IFusionProfileResolver profileResolver)
            {
                this.dbContext = dbContext;
                this.profileResolver = profileResolver;
            }

            public async Task<IEnumerable<QueryPersonNote>> Handle(GetPersonNotes request, CancellationToken cancellationToken)
            {
                var notes = await dbContext.PersonNotes
                    .Include(p => p.UpdatedBy)
                    .Where(p => p.AzureUniqueId == request.AzureUniqueId)
                    .ToListAsync(cancellationToken);

                return notes.Select(n => new QueryPersonNote(n));
            }
        }
    }
}
