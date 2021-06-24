using Fusion.Integration;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
            private readonly ResourcesDbContext resourcesDb;
            private readonly IHttpClientFactory httpClientFactory;

            public Handler(ResourcesDbContext resourcesDb, IHttpClientFactory httpClientFactory)
            {
                this.resourcesDb = resourcesDb;
                this.httpClientFactory = httpClientFactory;
            }

            protected override async Task Handle(UpdateContractPersonnelContactMail request, CancellationToken cancellationToken)
            {
                var ids = request.Mails.Select(m => m.personnelId).ToList();

                var persons = await resourcesDb.ContractPersonnel.Where(p => p.Contract.OrgContractId == request.OrgContractId && ids.Contains(p.Person.Id))
                    .Select(p => new { p.Person.Id, p.Person.AzureUniqueId })
                    .ToListAsync();


                var pplClient = httpClientFactory.CreateClient(IntegrationConfig.HttpClients.ApplicationPeople());
                foreach (var person in persons.Where(p => p.AzureUniqueId.HasValue))
                {
                    var mail = request.Mails.FirstOrDefault(m => m.personnelId == person.Id);
                    var resp = await pplClient.PatchAsJsonAsync($"/persons/{person.AzureUniqueId}/extended-profile?api-version=3.0", new
                    {
                        preferredContactMail = mail.preferredMail
                    });

                }
            }
        }

    }
}
