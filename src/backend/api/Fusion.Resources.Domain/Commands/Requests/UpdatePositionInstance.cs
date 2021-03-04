using Fusion.ApiClients.Org;
using MediatR;
using System;
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
        public UpdatePositionInstance(Guid orgProjectId, Guid positionId, Guid positionInstanceId, PatchPositionInstanceV2 instance)
        {
            OrgProjectId = orgProjectId;
            PositionId = positionId;
            PositionInstanceId = positionInstanceId;
            Instance = instance;
        }
        public Guid OrgProjectId { get; }
        public Guid PositionId { get; }
        public Guid PositionInstanceId { get; }
        public PatchPositionInstanceV2 Instance { get; }
        
        public class Handler : IRequestHandler<UpdatePositionInstance, ApiPositionV2>
        {
            private readonly IOrgApiClient orgClient;

            public Handler(IOrgApiClientFactory apiClientFactory)
            {
                this.orgClient = apiClientFactory.CreateClient(ApiClientMode.Application);
            }

            public async Task<ApiPositionV2> Handle(UpdatePositionInstance request, CancellationToken cancellationToken)
            {
                var position = await orgClient.GetPositionV2Async(request.OrgProjectId, request.PositionId);

                var resp = await orgClient.PatchPositionInstanceAsync(position, request.PositionInstanceId, request.Instance);

                if (resp.IsSuccessStatusCode)
                    return resp.Value;

                throw new OrgApiError(resp.Response, resp.Content);

            }
        }
    }
}
