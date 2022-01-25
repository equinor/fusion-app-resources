using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class ReplaceContractPersonnel : TrackableRequest<QueryContractPersonnel>
    {
        public ReplaceContractPersonnel(Guid projectId, Guid contractIdentifier, PersonnelId fromPerson, string toUpn, PersonnelId toPerson)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
            FromPerson = fromPerson;
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
        public bool ForceUpdate { get; private set; }


        public class Handler : IRequestHandler<ReplaceContractPersonnel, QueryContractPersonnel>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext resourcesDb, IMediator mediator)
            {
                this.resourcesDb = resourcesDb;
                this.mediator = mediator;
            }

            public async Task<QueryContractPersonnel> Handle(ReplaceContractPersonnel request, CancellationToken cancellationToken)
            {
                var existingPerson = await resourcesDb.ContractPersonnel
                    .GetById(request.OrgContractId, request.FromPerson)
                    .Include(cp => cp.Person)
                    .FirstOrDefaultAsync();

                if (existingPerson is null)
                    throw new InvalidOperationException($"Cannot locate person using personnel identifier '{request.FromPerson.OriginalIdentifier}'");
                if (existingPerson.Person.IsDeleted == false)
                    throw new InvalidOperationException($"Cannot replace person using personnel identifier '{request.FromPerson.OriginalIdentifier}' when account is not expired");

                var newPerson = await resourcesDb.ExternalPersonnel.GetById(request.ToPerson).FirstOrDefaultAsync();

                if (newPerson is null)
                    throw new InvalidOperationException($"Cannot locate person using personnel identifier '{request.ToPerson.OriginalIdentifier}'");

                if (!request.ForceUpdate)
                    if (!string.Equals(existingPerson.Person.UPN, newPerson.UPN, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Cannot update person having different UPN. Existing:{existingPerson.Person.UPN}, New:{newPerson.UPN}");

                existingPerson.Person = newPerson;

                existingPerson.Updated = DateTimeOffset.UtcNow;
                existingPerson.UpdatedBy = request.Editor.Person;


                await resourcesDb.SaveChangesAsync();

                var returnItem = await mediator.Send(new GetContractPersonnelItem(request.OrgContractId, request.ToPerson));
                return returnItem;
            }

        }
    }
}
