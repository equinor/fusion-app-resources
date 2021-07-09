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
        public SetPeoplePreferredContractMail(Guid azureUniqueId, string? preferredContactMail)
        {
            AzureUniqueId = azureUniqueId;
            PreferredContactMail = preferredContactMail;
        }

        public Guid AzureUniqueId { get; set; }
        public string? PreferredContactMail { get; set; }

        public class Handler : AsyncRequestHandler<SetPeoplePreferredContractMail>
        {
            private readonly IPeopleIntegration peopleIntegration;

            public Handler(IPeopleIntegration peopleIntegration)
            {
                this.peopleIntegration = peopleIntegration;
            }

            protected override async Task Handle(SetPeoplePreferredContractMail request, CancellationToken cancellationToken)
            {
                await peopleIntegration.UpdatePreferredContactMailAsync(request.AzureUniqueId, request.PreferredContactMail);
            }
        }
    }
}
