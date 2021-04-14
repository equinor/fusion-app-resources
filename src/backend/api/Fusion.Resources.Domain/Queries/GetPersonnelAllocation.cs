using Fusion.Integration;
using Fusion.Integration.Http;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetPersonnelAllocation : IRequest<QueryInternalPersonnelPerson?>
    {
        public GetPersonnelAllocation(PersonnelId personId)
        {
            PersonId = personId;
        }

        public PersonnelId PersonId { get; }

        public class Handler : IRequestHandler<GetPersonnelAllocation, QueryInternalPersonnelPerson?>
        {
            private readonly ILogger<Handler> logger;
            private readonly ResourcesDbContext db;
            private readonly IHttpClientFactory httpClientFactory;
            private readonly IMediator mediator;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ILogger<Handler> logger, ResourcesDbContext db, IHttpClientFactory httpClientFactory, IMediator mediator, IFusionProfileResolver profileResolver)
            {
                this.logger = logger;
                this.db = db;
                this.httpClientFactory = httpClientFactory;
                this.mediator = mediator;
                this.profileResolver = profileResolver;
            }

            public async Task<QueryInternalPersonnelPerson?> Handle(GetPersonnelAllocation request, CancellationToken cancellationToken)
            {
                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);

                var user = await profileResolver.ResolvePersonBasicProfileAsync(request.PersonId);
                if (user is null)
                    return null;

                if (user.AzureUniqueId is null)
                    throw new InvalidOperationException("Profile must have a azure unique id");

                var personWithAllocations = await PeopleSearchUtils.GetPersonFromSearchIndexAsync(peopleClient, user.AzureUniqueId.Value);

                if (personWithAllocations is null)
                    return null;

                var absence = await mediator.Send(new GetPersonAbsence(request.PersonId));

                personWithAllocations.Absence = absence.Select(a => new QueryPersonAbsenceBasic(a)).ToList();
                return personWithAllocations;
            }
        }
    }
}
