using Fusion.ApiClients.Org;
using MediatR;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    /// <summary>
    /// Update a position
    /// This will update a single instance position. If the position is detected to have multi instances etc, an error will be thrown.
    /// </summary>
    public class UpdatePositionInstance : TrackableRequest<ApiPositionV2>
    {
        public UpdatePositionInstance(Guid orgProjectId, ApiPositionInstanceV2 instance)
        {
            OrgProjectId = orgProjectId;
            Instance = instance;
        }
        public Guid OrgProjectId { get; }

        public ApiPositionInstanceV2 Instance { get; }


        public class Handler : IRequestHandler<UpdatePositionInstance, ApiPositionV2>
        {
            private readonly IOrgApiClient orgClient;

            public Handler(IOrgApiClientFactory apiClientFactory)
            {
                this.orgClient = apiClientFactory.CreateClient(ApiClientMode.Application);
            }

            public async Task<ApiPositionV2> Handle(UpdatePositionInstance request, CancellationToken cancellationToken)
            {
                var position = await orgClient.GetPositionV2Async(request.OrgProjectId, request.Instance.PositionId);

                var instance = position.Instances.First(x => x.Id == request.Instance.Id);
                
                var resp = await orgClient.PatchPositionInstanceAsync(position, instance);

                if (resp.IsSuccessStatusCode)
                    return resp.Value;

                throw new OrgApiError(resp.Response, resp.Content);

            }

            private static InvalidOperationException GenerateMultiInstanceError(ApiPositionV2 position)
            {
                var instances = string.Join(", ", position.Instances.OrderBy(i => i.AppliesFrom).Select(i => $"{i.AppliesFrom:yyyy-MM-dd} -> {i.AppliesTo:yyyy-MM-dd}"));
                return new InvalidOperationException($"Cannot update a position with multiple instances. Detected {position.Instances.Count} instances on {position.Name}, {instances}");
            }
        }
    }
}
