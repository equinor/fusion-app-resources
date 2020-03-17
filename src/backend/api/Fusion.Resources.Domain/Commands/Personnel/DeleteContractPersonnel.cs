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
    public class DeleteContractPersonnel : IRequest
    {
        public DeleteContractPersonnel(Guid projectId, Guid contractIdentifier, PersonnelId personnelId)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
            PersonnelId = personnelId;
        }

        public Guid OrgContractId { get; set; }
        public Guid OrgProjectId { get; set; }

        public PersonnelId PersonnelId { get; set; }


        public class Handler : AsyncRequestHandler<DeleteContractPersonnel>
        {
            private readonly IProfileService profileService;
            private readonly ResourcesDbContext resourcesDb;

            public Handler(IProfileService profileService, ResourcesDbContext resourcesDb)
            {
                this.profileService = profileService;
                this.resourcesDb = resourcesDb;
            }

            protected override async Task Handle(DeleteContractPersonnel request, CancellationToken cancellationToken)
            {
                var dbEntity = await resourcesDb.ContractPersonnel
                    .GetById(request.OrgContractId, request.PersonnelId)
                    .FirstOrDefaultAsync();

                if (dbEntity != null)
                {
                    resourcesDb.ContractPersonnel.Remove(dbEntity);

                    // Check if the person is used in any other contract, if not remove that too
                    var isStillInUse = await resourcesDb.ContractPersonnel.AnyAsync(c => c.PersonId == dbEntity.PersonId);
                    if (isStillInUse == false)
                    {
                        var personnelItem = await resourcesDb.ExternalPersonnel.FirstAsync(p => p.Id == dbEntity.PersonId);
                        resourcesDb.ExternalPersonnel.Remove(personnelItem);
                    }
                }

                await resourcesDb.SaveChangesAsync();
            }
        }
    }
}
