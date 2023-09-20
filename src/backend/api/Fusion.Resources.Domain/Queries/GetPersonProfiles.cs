using Fusion.Integration;
using Fusion.Integration.Profile;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetPersonProfiles : IRequest<Dictionary<Guid, FusionPersonProfile>>
    {
        public GetPersonProfiles(IEnumerable<Guid> azureObjectIds)
        {
            Identifiers = azureObjectIds
                .Distinct()
                .Select(aadId => new PersonIdentifier(aadId))
                .ToList();
        }

        public List<PersonIdentifier> Identifiers { get; }

        public class Handler : IRequestHandler<GetPersonProfiles, Dictionary<Guid, FusionPersonProfile>>
        {
            private readonly IFusionProfileResolver profileResolver;

            public Handler(IFusionProfileResolver profileResolver)
            {
                this.profileResolver = profileResolver;
            }
            public async Task<Dictionary<Guid, FusionPersonProfile>> Handle(GetPersonProfiles request, CancellationToken cancellationToken)
            {
                var tasks = new List<Task<IEnumerable<ResolvedPersonProfile>>>();

                // Max number of identifiers is 500, so we chunk the requests
                foreach (var req in request.Identifiers.Chunk(500))
                {
                    tasks.Add(profileResolver.ResolvePersonsAsync(request.Identifiers));
                }

                var results = await Task.WhenAll(tasks);

                var profiles = results.Select(X => X).SelectMany(o => o);

                return profiles
                    .Where(p => p.Success && p.Profile?.AzureUniqueId != null)
                    .ToDictionary(
                        p => p.Profile!.AzureUniqueId!.Value,
                        p => p.Profile!
                    );
            }
        }
    }
}
