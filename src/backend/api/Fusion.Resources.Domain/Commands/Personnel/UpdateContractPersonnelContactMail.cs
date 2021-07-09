using Fusion.Integration;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class UpdateContractPersonnelContactMail : TrackableRequest
    {
        public UpdateContractPersonnelContactMail(Guid orgContractId, IEnumerable<(Guid personnelId, string? preferredMail)> mails)
        {
            OrgContractId = orgContractId;
            Mails = mails;
        }

        public Guid OrgContractId { get; set; }
        public IEnumerable<(Guid personnelId, string? preferredMail)> Mails { get; set; }


        public class Handler : AsyncRequestHandler<UpdateContractPersonnelContactMail>
        {
            private readonly ILogger<Handler> logger;
            private readonly ResourcesDbContext resourcesDb;
            private readonly IPeopleIntegration peopleIntegration;

            public Handler(ILogger<Handler> logger, ResourcesDbContext resourcesDb, IPeopleIntegration peopleIntegration)
            {
                this.logger = logger;
                this.resourcesDb = resourcesDb;
                this.peopleIntegration = peopleIntegration;
            }

            protected override async Task Handle(UpdateContractPersonnelContactMail request, CancellationToken cancellationToken)
            {
                var ids = request.Mails.Select(m => m.personnelId).ToList();

                var persons = await resourcesDb.ContractPersonnel
                    .Where(p => p.Contract.OrgContractId == request.OrgContractId && ids.Contains(p.Person.Id))
                    .Select(p => p.Person)
                    .ToListAsync();

                foreach (var person in persons.Where(p => p.AzureUniqueId.HasValue))
                {
                    try
                    {
                        var mail = request.Mails.FirstOrDefault(m => m.personnelId == person.Id);
                        await peopleIntegration.UpdatePreferredContactMailAsync(person.AzureUniqueId!.Value, mail.preferredMail);

                        person.PreferredContractMail = mail.preferredMail;
                    }
                    catch (Exception ex)
                    {
                        // Instead of failing the whole batch and complexions with partial updates etc, we should just log the issues. 
                        // It will be visible for the user in the way of the mail not being updated.
                        logger.LogError(ex, $"Could not update preferred mail in people service: {ex.Message}");
                    }
                }

                await resourcesDb.SaveChangesAsync();
            }
        }

    }
}
