using Fusion.Resources.Database;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ContractorPersonnelRequest
    {
        public class Delete : TrackableRequest
        {
            public Delete(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }

            public class Handler : AsyncRequestHandler<Delete>
            {
                private readonly ResourcesDbContext resourcesDb;

                public Handler(ResourcesDbContext resourcesDb)
                {
                    this.resourcesDb = resourcesDb;
                }
                protected override async Task Handle(Delete request, CancellationToken cancellationToken)
                {
                    var dbEntity = await resourcesDb.ContractorRequests.FirstOrDefaultAsync(c => c.Id == request.RequestId);

                    if (dbEntity != null)
                    {
                        resourcesDb.ContractorRequests.Remove(dbEntity);
                    }

                    await resourcesDb.SaveChangesAsync();
                }
            }
        }

    }


}
