using Fusion.Integration;
using MediatR;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    /// <summary>
    /// Update the preferred contact mail in the people service.
    /// </summary>
    public class SetPeoplePreferredContractMail : IRequest
    {
        public SetPeoplePreferredContractMail(Guid azureUniqueId, string preferredContactMail)
        {
            AzureUniqueId = azureUniqueId;
            PreferredContactMail = preferredContactMail;
        }

        public Guid AzureUniqueId { get; set; }
        public string? PreferredContactMail { get; set; }

        public class Handler : AsyncRequestHandler<SetPeoplePreferredContractMail>
        {
            private readonly HttpClient client;

            public Handler(IHttpClientFactory httpClientFactory)
            {
                this.client = httpClientFactory.CreateClient(IntegrationConfig.HttpClients.ApplicationPeople());
            }
            protected override async Task Handle(SetPeoplePreferredContractMail request, CancellationToken cancellationToken)
            {
                
                var resp = await client.PatchAsJsonAsync($"/persons/{request.AzureUniqueId}/extended-profile?api-version=3.0", new
                {
                    preferredContactMail = request.PreferredContactMail
                });
            }
        }
    }
}
