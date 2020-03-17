using Fusion.Integration;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class UpdateContractPersonnel : TrackableRequest<QueryContractPersonnel>
    {
        public UpdateContractPersonnel(Guid projectId, Guid contractIdentifier, PersonnelId personnelId)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
            PersonnelId = personnelId;
        }

        public Guid OrgContractId { get; set; }
        public Guid OrgProjectId { get; set; }
        public PersonnelId PersonnelId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string JobTitle { get; set; }
        public string Phone { get; set; }
        public List<string> Disciplines { get; set; } = new List<string>();

        public Guid EditorAzureUniqueId { get; set; }


        public class Handler : IRequestHandler<UpdateContractPersonnel, QueryContractPersonnel>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext resourcesDb, IMediator mediator)
            {
                this.resourcesDb = resourcesDb;
                this.mediator = mediator;
            }

            public async Task<QueryContractPersonnel> Handle(UpdateContractPersonnel request, CancellationToken cancellationToken)
            {
                var contractPersonnel = await resourcesDb.ContractPersonnel
                    .GetById(request.OrgContractId, request.PersonnelId)
                    .Include(cp => cp.Person).ThenInclude(p => p.Disciplines)
                    .FirstOrDefaultAsync();

                if (contractPersonnel is null)
                    throw new ArgumentException($"Cannot locate person using personnel identifier '{request.PersonnelId.OriginalIdentifier}'");


                UpdatePerson(contractPersonnel.Person, request);

                contractPersonnel.Updated = DateTimeOffset.UtcNow;
                contractPersonnel.UpdatedBy = request.Editor.Person;
                

                await resourcesDb.SaveChangesAsync();

                var returnItem = await mediator.Send(new GetContractPersonnelItem(request.OrgContractId, contractPersonnel.PersonId));
                return returnItem;
            }

            private void UpdatePerson(DbExternalPersonnelPerson dbPersonnel, UpdateContractPersonnel request)
            {
                dbPersonnel.Name = $"{request.FirstName} {request.LastName}";
                dbPersonnel.FirstName = request.FirstName;
                dbPersonnel.LastName = request.LastName;
                dbPersonnel.JobTitle = request.JobTitle;
                dbPersonnel.Phone = request.Phone;
                dbPersonnel.Disciplines = request.Disciplines?.Select(d => new DbPersonnelDiscipline { Name = d }).ToList() ?? new List<DbPersonnelDiscipline>();
            }
        }
    }
}
