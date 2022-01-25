using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain.Commands
{
    public class ReplaceContractPersonnel : TrackableRequest<QueryContractPersonnel>
    {
        public ReplaceContractPersonnel(Guid projectId, Guid contractIdentifier, PersonnelId fromPerson, string toUpn, PersonnelId toPerson)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
            FromPerson = fromPerson;
            ToUpn = toUpn;
            ToPerson = toPerson;
        }

        public ReplaceContractPersonnel WithForce(bool force)
        {
            ForceUpdate = force;
            return this;
        }

        public Guid OrgContractId { get; }
        public Guid OrgProjectId { get; }
        public PersonnelId FromPerson { get; }
        public PersonnelId ToPerson { get; }
        public string ToUpn { get; }
        public bool ForceUpdate { get; private set; }


        public class Handler : IRequestHandler<ReplaceContractPersonnel, QueryContractPersonnel>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IMediator mediator;
            private readonly IProfileService profileService;

            public Handler(ResourcesDbContext resourcesDb, IMediator mediator, IProfileService profileService)
            {
                this.resourcesDb = resourcesDb;
                this.mediator = mediator;
                this.profileService = profileService;
            }

            public async Task<QueryContractPersonnel> Handle(ReplaceContractPersonnel request, CancellationToken cancellationToken)
            {
                var existingPerson = await resourcesDb.ContractPersonnel
                    .GetById(request.OrgContractId, request.FromPerson)
                    .Include(cp => cp.Person)
                    .FirstOrDefaultAsync();

                var existingPersonPreferredContractMail = existingPerson.Person.PreferredContractMail;

                var newPerson = await ValidatePersonAsync(request, existingPerson);

                existingPerson.Person = newPerson;

                existingPerson.Updated = DateTimeOffset.UtcNow;
                existingPerson.UpdatedBy = request.Editor.Person;
                
                await resourcesDb.SaveChangesAsync();


                if (string.Equals(existingPerson.Person.UPN, newPerson.UPN, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(existingPersonPreferredContractMail))
                {
                    // Update newPerson's preferred email in PPL service, since found on existing account matched by UPN
                    var mail = new List<(Guid personnelId, string? preferredMail)> { new(newPerson.Id, existingPersonPreferredContractMail) };
                    await mediator.Send(new UpdateContractPersonnelContactMail(request.OrgContractId, mail));
                }
                
                var returnItem = await mediator.Send(new GetContractPersonnelItem(request.OrgContractId, request.ToPerson));
                return returnItem;
            }

            private async Task<DbExternalPersonnelPerson> ValidatePersonAsync(ReplaceContractPersonnel request, DbContractPersonnel existingPerson)
            {
                if (existingPerson is null)
                    throw new InvalidOperationException(
                        $"Cannot locate person using personnel identifier '{request.FromPerson.OriginalIdentifier}'");
                if (existingPerson.Person.IsDeleted == false)
                    throw new InvalidOperationException(
                        $"Cannot replace person using personnel identifier '{request.FromPerson.OriginalIdentifier}' when account is not expired");

                var newPerson = await profileService.EnsureExternalPersonnelAsync(request.ToUpn, request.ToPerson,
                    existingPerson.Person.FirstName, existingPerson.Person.LastName);
                if (existingPerson.Person.Id == newPerson.Id)
                    throw new InvalidOperationException(
                        $"Cannot replace person using personnel identifier '{request.FromPerson.OriginalIdentifier}'. New account identified as existing account");

                if (newPerson is null)
                    throw new InvalidOperationException(
                        $"Cannot locate person using personnel identifier '{request.ToPerson.OriginalIdentifier}'");

                if (!request.ForceUpdate)
                    if (!string.Equals(existingPerson.Person.UPN, newPerson.UPN, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            $"Cannot update person having different UPN. Existing:{existingPerson.Person.UPN}, New:{newPerson.UPN}");

                return newPerson!;
            }
        }
    }
}
