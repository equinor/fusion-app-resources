using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    /// <summary>
    /// Resolve the manager for the person.
    /// </summary>
    public class GetResourceOwner : IRequest<FusionPersonProfile?>
    {
        public GetResourceOwner(Guid personAzureUniqueId)
        {
            AzureUniqueId = personAzureUniqueId;
        }

        public Guid AzureUniqueId { get; set; }

        public class Handler : IRequestHandler<GetResourceOwner, FusionPersonProfile?>
        {
            private readonly IFusionProfileResolver profileResolver;

            public Handler(IFusionProfileResolver profileResolver)
            {
                this.profileResolver = profileResolver;
            }

            public async Task<FusionPersonProfile?> Handle(GetResourceOwner request, CancellationToken cancellationToken)
            {
                var profile = await profileResolver.ResolvePersonBasicProfileAsync(request.AzureUniqueId);
                if (profile is null || profile.ManagerAzureUniqueId is null)
                    return null;

                var manager = await profileResolver.ResolvePersonBasicProfileAsync(profile.ManagerAzureUniqueId.Value);
                return manager;
            }
        }
    }
}
