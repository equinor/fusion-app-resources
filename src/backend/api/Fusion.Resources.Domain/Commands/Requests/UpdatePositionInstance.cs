using Fusion.ApiClients.Org;
using MediatR;
using System;
using System.Collections.Generic;
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
        public Guid PositionId { get; set; }
        public Guid PositionInstanceId { get; set; }

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
    public class PatchPositionInstanceV2
    {
        public string? Type { get; set; }
        public string? ExternalId { get; set; }
        public DateTime? AppliesFrom { get; set; }
        public DateTime? AppliesTo { get; set; }
        public double? Workload { get; set; }
        public string? Obs { get; set; }
        public bool? IsPrimary { get; set; }
        public string? Calendar { get; set; }
        public string? RotationId { get; set; }
        public ApiPositionLocationV2? Location { get; set; }
        public ApiPersonV2? AssignedPerson { get; set; }
        public Guid? ParentPositionId { get; set; }
        public List<Guid>? TaskOwnerIds { get; set; }
        public ApiPropertiesCollectionV2? Properties { get; set; }
    }
}
