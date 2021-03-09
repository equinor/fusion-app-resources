using Fusion.Integration;
using Fusion.Integration.Profile;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetResourceOwner : IRequest<FusionPersonProfile?>
    {
        public GetResourceOwner(Guid personAzureUniqueId)
        {
            AzureUniqueId = personAzureUniqueId;
        }

        public Guid AzureUniqueId { get; set; }

        public class Handler : IRequestHandler<GetResourceOwner, FusionPersonProfile?>
        {
            private readonly HttpClient client;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(IHttpClientFactory httpClientFactory, IFusionProfileResolver profileResolver)
            {
                this.client = httpClientFactory.CreateClient("lineorg");
                this.profileResolver = profileResolver;
            }
            public async Task<FusionPersonProfile?> Handle(GetResourceOwner request, CancellationToken cancellationToken)
            {
                var resp = await client.GetAsync($"lineorg/persons/{request.AzureUniqueId}");
                var content = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    var profile = JsonConvert.DeserializeAnonymousType(content, new { ManagerId = (Guid?)null });
                    if (profile.ManagerId.HasValue)
                    {
                        var fusionProfile = await profileResolver.ResolvePersonBasicProfileAsync(profile.ManagerId.Value);
                        return fusionProfile;
                    }

                }

                return null;
            }
        }
    }
}
