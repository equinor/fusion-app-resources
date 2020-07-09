using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable 

namespace Fusion.Resources.Domain.Commands
{

    public class CreateContractPersonnel : TrackableRequest<QueryContractPersonnel>
    {
        public CreateContractPersonnel(Guid projectId, Guid contractIdentifier, string mail)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
            Person = mail;
        }

        public Guid OrgContractId { get; set; }
        public Guid OrgProjectId { get; set; }

        public PersonId Person { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string Phone { get; set; } = string.Empty;

        public string? DawinciCode { get; set; }
        public string? LinkedInProfile { get; set; }

        public List<string> Disciplines { get; set; } = new List<string>();

        public class Handler : IRequestHandler<CreateContractPersonnel, QueryContractPersonnel>
        {
            private readonly IProfileService profileService;
            private readonly ResourcesDbContext resourcesDb;
            private readonly IProjectOrgResolver resolver;
            private readonly IMediator mediator;

            public Handler(IProfileService profileService, ResourcesDbContext resourcesDb, IProjectOrgResolver resolver, IMediator mediator)
            {
                this.profileService = profileService;
                this.resourcesDb = resourcesDb;
                this.resolver = resolver;
                this.mediator = mediator;
            }

            public async Task<QueryContractPersonnel> Handle(CreateContractPersonnel request, CancellationToken cancellationToken)
            {
                var profile = await profileService.ResolveProfileAsync(request.Person);
                if (profile == null && request.Person.Mail == null)
                    throw new ArgumentException("Cannot create personnel without either a valid azure unique id or mail address");

                var personnel = await profileService.EnsureExternalPersonnelAsync(profile?.Mail ?? request.Person.Mail!, request.FirstName, request.LastName);

                // Even if the personnel is fetch from existing. Update to new values, as things might change, like phone number.
                UpdatePerson(personnel, request);

                // Validate references.
                var project = await resourcesDb.Projects.FirstOrDefaultAsync(p => p.OrgProjectId == request.OrgProjectId);
                var contract = await resourcesDb.Contracts.FirstOrDefaultAsync(c => c.OrgContractId == request.OrgContractId);

                if (project is null)
                    throw new InvalidOperationException("Could not locate the project, does it have any contracts allocated?");

                if (contract is null)
                    throw new InvalidOperationException($"Cannot create personnel to unallocated contracts. Could not locate any contracts with id {request.OrgContractId}.");

                // Check for existing personnel entries for the contract
                var existingItem = await resourcesDb.ContractPersonnel.Include(c => c.CreatedBy).FirstOrDefaultAsync(c => c.PersonId == personnel.Id && c.ProjectId == project.Id && c.ContractId == contract.Id);
                if (existingItem != null)
                    throw new InvalidOperationException($"The specified person is already added to the current contract. Added @ {existingItem.Created} by {existingItem.CreatedBy.Mail}");

                await EnsureUserNotAllocatedToOtherCompanies(personnel, project, contract);

                var newItem = new DbContractPersonnel
                {
                    Project = project,
                    Contract = contract,
                    Person = personnel,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = request.Editor.Person
                };
                await resourcesDb.ContractPersonnel.AddAsync(newItem);

                await resourcesDb.SaveChangesAsync();

                await mediator.Publish(new Notifications.PersonnelAddedToContract(newItem.Id));

                return new QueryContractPersonnel(newItem);
            }

            private async Task EnsureUserNotAllocatedToOtherCompanies(DbExternalPersonnelPerson personnel, DbProject project, DbContract contract)
            {
                var newContractInfo = await resolver.ResolveContractAsync(project.OrgProjectId, contract.OrgContractId);

                if (newContractInfo is null)
                    throw new InvalidOperationException($"Could not locate the new contract info in Pro Org service. Aborting request creation.");

                //check that this user profile does not exist in other companies
                var existingContractIds = await resourcesDb.ContractPersonnel
                    .Where(c => c.PersonId == personnel.Id)
                    .Select(c => c.Contract.OrgContractId)
                    .ToListAsync();

                foreach (var contractId in existingContractIds)
                {
                    var existingContract = await resolver.ResolveContractAsync(project.OrgProjectId, contract.OrgContractId);

                    if (existingContract == null)
                        throw new InvalidOperationException($"Could not find contract '{contractId}' for existing allocation in Pro Org service. Aborting request creation.");

                    if (newContractInfo.Company.Id != existingContract.Company.Id)
                        throw new InvalidOperationException($"Personnel is allocated to contract belonging to company '{existingContract.Company.Name}'. " +
                            $"He/she cannot be allocated to this contract, which is for company '{newContractInfo.Company.Name}'");
                }
            }

            private void UpdatePerson(DbExternalPersonnelPerson dbPersonnel, CreateContractPersonnel request)
            {
                dbPersonnel.Name = $"{request.FirstName} {request.LastName}";
                dbPersonnel.FirstName = request.FirstName;
                dbPersonnel.LastName = request.LastName;
                dbPersonnel.JobTitle = request.JobTitle;
                dbPersonnel.Phone = request.Phone;
                dbPersonnel.DawinciCode = request.DawinciCode;
                dbPersonnel.LinkedInProfile = request.LinkedInProfile;
                dbPersonnel.Disciplines = request.Disciplines?.Select(d => new DbPersonnelDiscipline { Name = d }).ToList() ?? new List<DbPersonnelDiscipline>();
            }
        }
    }
}
