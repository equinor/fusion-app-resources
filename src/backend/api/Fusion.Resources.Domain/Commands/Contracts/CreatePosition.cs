using Fusion.ApiClients.Org;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    /// <summary>
    /// Will create a new position
    /// </summary>
    public class CreatePosition : TrackableRequest<ApiPositionV2>
    {
        public CreatePosition(Guid projectId)
        {
            OrgProjectId = projectId;
        }

        public Guid OrgProjectId { get; }
        public Guid BasePositionId { get; set; }
        public string? PositionName { get; set; }
        public string? ExternalId { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public double Workload { get; set; }
        public Guid? ParentPositionId { get; set; }
        public PersonId? AssignedPerson { get; set; }
        public string? Obs { get; set; }

        public class Handler : IRequestHandler<CreatePosition, ApiPositionV2>
        {
            private readonly IOrgApiClient orgClient;

            public Handler(IOrgApiClientFactory apiClientFactory)
            {
                this.orgClient = apiClientFactory.CreateClient(ApiClientMode.Application);
            }

            public async Task<ApiPositionV2> Handle(CreatePosition request, CancellationToken cancellationToken)
            {

                var newPosition = GeneratePositionEntity(request);

                var createResp = await orgClient.CreatePositionAsync(request.OrgProjectId, newPosition);

                if (createResp.IsSuccessStatusCode)
                    return createResp.Value;

                throw new OrgApiError(createResp.Response, createResp.Content);
            }

            private ApiPositionV2 GeneratePositionEntity(CreatePosition request) => new ApiPositionV2
            {
                BasePosition = new ApiPositionBasePositionV2 { Id = request.BasePositionId },
                Name = request.PositionName,
                ExternalId = request.ExternalId,
                Instances = new List<ApiPositionInstanceV2>
                    {
                        new ApiPositionInstanceV2
                        {
                            AppliesFrom = request.AppliesFrom,
                            AppliesTo = request.AppliesTo,
                            Workload = request.Workload,
                            Obs = request.Obs,
                            AssignedPerson =  request.AssignedPerson,
                            ParentPositionId = request.ParentPositionId
                        }
                    }
            };
        }

    }
}
