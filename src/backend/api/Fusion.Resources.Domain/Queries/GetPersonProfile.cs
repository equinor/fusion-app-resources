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
            PersonId = new PersonId(personAzureUniqueId);
        }
        public GetPersonProfile(PersonId personId)
        {
            PersonId = personId;
        }

        public PersonId PersonId { get; }

        public class Handler : IRequestHandler<GetPersonProfile, FusionPersonProfile?>
        {
            private readonly IFusionProfileResolver profileResolver;

            public Handler(IFusionProfileResolver profileResolver)
            {
                this.profileResolver = profileResolver;
            }
            public async Task<FusionPersonProfile?> Handle(GetPersonProfile request, CancellationToken cancellationToken)
            {
                var fusionProfile = await profileResolver.ResolvePersonBasicProfileAsync(request.PersonId);
                return fusionProfile;
            }
        }
    }
}
