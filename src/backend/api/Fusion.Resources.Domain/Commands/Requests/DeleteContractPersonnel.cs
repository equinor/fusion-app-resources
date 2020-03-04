using Fusion.Integration;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class DeleteContractPersonnelRequest : IRequest
    {
        public DeleteContractPersonnelRequest(Guid projectId, Guid contractIdentifier, Guid requestId)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
            RequestId = requestId;
        }

        public Guid OrgContractId { get; set; }
        public Guid OrgProjectId { get; set; }

        public Guid RequestId { get; set; }


        public class Handler : AsyncRequestHandler<DeleteContractPersonnelRequest>
        {
            private readonly IProfileService profileService;
            private readonly ResourcesDbContext resourcesDb;

            public Handler(IProfileService profileService, ResourcesDbContext resourcesDb)
            {
                this.profileService = profileService;
                this.resourcesDb = resourcesDb;
            }

            protected override async Task Handle(DeleteContractPersonnelRequest request, CancellationToken cancellationToken)
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
