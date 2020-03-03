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
    public class UpdateContractPersonnel : IRequest<QueryContractPersonnel>
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

            public Handler(ResourcesDbContext resourcesDb)
            {
                this.resourcesDb = resourcesDb;
            }

            public async Task<QueryContractPersonnel> Handle(UpdateContractPersonnel request, CancellationToken cancellationToken)
            {
                var query = request.PersonnelId.Type switch
                {
                    PersonnelId.IdentifierType.UniqueId => resourcesDb.ContractPersonnel.Where(c => c.Id == request.PersonnelId.UniqueId || c.Person.AzureUniqueId == request.PersonnelId.UniqueId),
                    _ => resourcesDb.ContractPersonnel.Where(c => c.Person.Mail == request.PersonnelId.Mail)
                };

                var dbPersonnel = await query.Select(cp => cp.Person).FirstOrDefaultAsync();

                if (dbPersonnel is null)
                    throw new ArgumentException($"Cannot locate person using personnel identifier '{request.PersonnelId.OriginalIdentifier}'");


                UpdatePerson(dbPersonnel, request);
                await resourcesDb.SaveChangesAsync();


                var item = await query
                      .Include(i => i.Contract)
                      .Include(i => i.Project)
                      .Include(i => i.UpdatedBy)
                      .Include(i => i.CreatedBy)
                      .Include(i => i.Person).ThenInclude(p => p.Disciplines)
                      .FirstOrDefaultAsync();

                return new QueryContractPersonnel(item);
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
