using Fusion.ApiClients.Org;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Core.Http.Patch;

namespace Fusion.Resources.Domain.Commands
{
    /// <summary>
    /// Update a position
    /// This will update a single instance position. If the position is detected to have multi instances etc, an error will be thrown.
    /// </summary>
    public class UpdatePositionInstance : TrackableRequest<ApiPositionV2>
    {
        public UpdatePositionInstance(Guid orgProjectId, Guid positionId, Guid positionInstanceId, PatchApiPositionInstanceV2 instance)
        {
            OrgProjectId = orgProjectId;
            PositionId = positionId;
            PositionInstanceId = positionInstanceId;
            Instance = instance;
        }
        public Guid OrgProjectId { get; }
        public Guid PositionId { get; set; }
        public Guid PositionInstanceId { get; set; }

        public PatchApiPositionInstanceV2 Instance { get; }


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

        public class PatchApiPositionInstanceV2 : PatchRequest
        {
            public PatchProperty<string?> Type { get; set; } = null!;
            public PatchProperty<string?> ExternalId { get; set; } = null!;
            public PatchProperty<DateTime?> AppliesFrom { get; set; } = null!;
            public PatchProperty<DateTime?> AppliesTo { get; set; } = null!;
            public PatchProperty<double?> Workload { get; set; } = null!;
            public PatchProperty<string?> Obs { get; set; } = null!;
            public PatchProperty<bool?> IsPrimary { get; set; } = null!;
            public PatchProperty<string?> Calendar { get; set; } = null!;
            public PatchProperty<string?> RotationId { get; set; } = null!;
            public PatchProperty<ApiPositionLocationV2?> Location { get; set; } = null!;
            public PatchProperty<ApiPersonV2?> AssignedPerson { get; set; } = null!;
            public PatchProperty<Guid?> ParentPositionId { get; set; } = null!;
            public PatchProperty<List<Guid>?> TaskOwnerIds { get; set; } = null!;
            public PatchProperty<ApiPropertiesCollectionV2?> Properties { get; set; } = null!;
        }
    }
}
