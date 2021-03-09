using Fusion.Integration;
using Fusion.Integration.Profile;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetPersonProfile : IRequest<FusionPersonProfile?>
    {
        public GetPersonProfile(Guid personAzureUniqueId)
        {
            AzureUniqueId = personAzureUniqueId;
        }

        public Guid AzureUniqueId { get; set; }

        public class Handler : IRequestHandler<GetPersonProfile, FusionPersonProfile?>
        {
            private readonly IFusionProfileResolver profileResolver;

            public Handler(IFusionProfileResolver profileResolver)
            {
                this.profileResolver = profileResolver;
            }
            public async Task<FusionPersonProfile?> Handle(GetPersonProfile request, CancellationToken cancellationToken)
            {
                var fusionProfile = await profileResolver.ResolvePersonBasicProfileAsync(request.AzureUniqueId);
                return fusionProfile;
            }
        }
    }
}
